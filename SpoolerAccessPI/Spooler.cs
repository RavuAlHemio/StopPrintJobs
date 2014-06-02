// Released into the public domain.
// http://creativecommons.org/publicdomain/zero/1.0/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpoolerAccessPI
{
    public class Spooler : IDisposable
    {
        private IntPtr StopSemaphore { get; set; }
        private bool IsDisposed { get; set; }

        public Spooler()
        {
            StopSemaphore = Natives.Kernel32.CreateSemaphore(IntPtr.Zero, 0, 1, null);
            if (StopSemaphore == IntPtr.Zero)
            {
                throw new InteropHelpers.NativeCodeException("failed to create stopping semaphore", "CreateSemaphore");
            }
            IsDisposed = false;
        }

        public static List<string> EnumLocalPrinters(bool sharedOnly = false)
        {
            uint searchFlags = Natives.Constants.PRINTER_ENUM_LOCAL;
            uint bytesNeeded, bytesReturned, itemsReturned;

            if (sharedOnly)
            {
                searchFlags |= Natives.Constants.PRINTER_ENUM_SHARED;
            }

            // see how much space we'll need
            if (!Natives.Winspool.EnumPrinters(searchFlags, null, 4, IntPtr.Zero, 0, out bytesNeeded, out itemsReturned))
            {
                if (Marshal.GetLastWin32Error() != Natives.Constants.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new InteropHelpers.NativeCodeException("failed to get space requirement for printer list", "EnumPrinters");
                }
            }

            // allocate that
            using (var printerInfoBytes = new InteropHelpers.HGlobalBytes((int)bytesNeeded))
            using (var printerInfoDeser = new InteropHelpers.StructArraySerializer<Natives.Structures.PRINTER_INFO_4>())
            {
                // fetch again
                if (!Natives.Winspool.EnumPrinters(searchFlags, null, 4, printerInfoBytes.Pointer, bytesNeeded, out bytesReturned, out itemsReturned))
                {
                    throw new InteropHelpers.NativeCodeException("failed to get printer list", "EnumPrinters");
                }

                // deserialize!
                printerInfoDeser.Deserialize((int)itemsReturned, printerInfoBytes.Pointer);

                var ret = new List<string>();
                foreach (var printerInfo in printerInfoDeser.TheStructs)
                {
                    ret.Add(printerInfo.PrinterName);
                }
                return ret;
            }
        }

        public void PauseNewJobsProc(List<string> printersToPause)
        {
            if (printersToPause == null || printersToPause.Count == 0)
            {
                return;
            }

            // fetch the printer handles
            using (var printerHandlesDisposer = InteropHelpers.HandleArrayDisposer.NewReturningBool(Natives.Winspool.ClosePrinter))
            {
                foreach (var printerName in printersToPause)
                {
                    IntPtr printerHandle;
                    if (!Natives.Winspool.OpenPrinter(printerName, out printerHandle, IntPtr.Zero) || printerHandle == Natives.Constants.INVALID_HANDLE_VALUE)
                    {
                        throw new InteropHelpers.NativeCodeException("failed to open printer " + printerName, "OpenPrinter2");
                    }
                    printerHandlesDisposer.Handles.Add(printerHandle);
                }

                // prepare notification options
                using (var fieldsArrayBytes = new InteropHelpers.HGlobalBytes(1 * sizeof(ushort)))
                using (var notifyOptionsTypeSerializer = new InteropHelpers.StructSerializer<Natives.Structures.PRINTER_NOTIFY_OPTIONS_TYPE>())
                using (var notifyOptionsSerializer = new InteropHelpers.StructSerializer<Natives.Structures.PRINTER_NOTIFY_OPTIONS>())
                {
                    Marshal.WriteInt16(fieldsArrayBytes.Pointer, (short)Natives.Constants.PRINTER_NOTIFY_FIELD_STATUS);

                    var notifyOptionsType = new Natives.Structures.PRINTER_NOTIFY_OPTIONS_TYPE {
                        Type = Natives.Constants.JOB_NOTIFY_TYPE,
                        Reserved0 = 0,
                        Reserved1 = 0,
                        Reserved2 = 0,
                        Count = 1,
                        Fields = fieldsArrayBytes.Pointer
                    };
                    notifyOptionsTypeSerializer.TheStruct = notifyOptionsType;
                    notifyOptionsTypeSerializer.Serialize();

                    var notifyOptions = new Natives.Structures.PRINTER_NOTIFY_OPTIONS {
                        Version = 2,
                        Flags = 0,
                        Count = 1,
                        Types = notifyOptionsTypeSerializer.StructPointer
                    };
                    notifyOptionsSerializer.TheStruct = notifyOptions;
                    notifyOptionsSerializer.Serialize();

                    // fetch the notifications
                    using (var notificationDisposer = InteropHelpers.HandleArrayDisposer.NewReturningBool(Natives.Winspool.FindClosePrinterChangeNotification))
                    {
                        foreach (var printerHandle in printerHandlesDisposer.Handles)
                        {
                            IntPtr notif = Natives.Winspool.FindFirstPrinterChangeNotification(printerHandle, Natives.Constants.PRINTER_CHANGE_ADD_JOB, 0, notifyOptionsSerializer.StructPointer);
                            if (notif == Natives.Constants.INVALID_HANDLE_VALUE)
                            {
                                throw new InteropHelpers.NativeCodeException("failed to subscribe to notifications for a printer", "FindFirstPrinterChangeNotification");
                            }
                            notificationDisposer.Handles.Add(notif);
                        }

                        // prepare the array for waiting
                        IntPtr[] waitHandles = new IntPtr[notificationDisposer.Handles.Count + 1];
                        waitHandles[0] = StopSemaphore;
                        notificationDisposer.Handles.CopyTo(waitHandles, 1);

                        // and we wait and we wonder
                        for (;;)
                        {
                            uint waitResult = Natives.Kernel32.WaitForMultipleObjects((uint)waitHandles.Length, waitHandles, false, Natives.Constants.INFINITE);
                            if (waitResult >= waitHandles.Length)
                            {
                                throw new InteropHelpers.NativeCodeException("waiting for printers failed", "WaitForMultipleObjects");
                            }
                            else if (waitResult == 0)
                            {
                                // stopping semaphore triggered
                                break;
                            }

                            IntPtr triggeredNotif = waitHandles[waitResult];
                            uint whatChanged;
                            IntPtr notifyInfoPointer;
                            if (!Natives.Winspool.FindNextPrinterChangeNotification(triggeredNotif, out whatChanged, notifyOptionsSerializer.StructPointer, out notifyInfoPointer))
                            {
                                throw new InteropHelpers.NativeCodeException("fetching change notification failed", "FindNextPrinterChangeNotification");
                            }

                            using (var notifyInfoDisposer = InteropHelpers.HandleArrayDisposer.NewReturningBool(Natives.Winspool.FreePrinterNotifyInfo))
                            using (var notifyInfoSerializer = new InteropHelpers.StructSerializer<Natives.Structures.PRINTER_NOTIFY_INFO>())
                            {
                                var notifyInfo = SpecialStructures.PrinterNotifyInfo.Deserialize(notifyInfoPointer);
                                if (notifyInfo.DataList.Count == 0)
                                {
                                    throw new InteropHelpers.NativeCodeException("add-job notification with no data?!", "FindNextPrinterChangeNotification");
                                }

                                if (notifyInfo.DataList[0].Type != Natives.Constants.JOB_NOTIFY_TYPE)
                                {
                                    throw new InteropHelpers.NativeCodeException("add-job notification with no job-related data?!", "FindNextPrinterChangeNotification");
                                }

                                // pause it
                                // (need to subtract 1 because 0 is the stopping semaphore)
                                if (!Natives.Winspool.SetJob(printerHandlesDisposer.Handles[(int)waitResult - 1], notifyInfo.DataList[0].ID, 0, IntPtr.Zero, Natives.Constants.JOB_CONTROL_PAUSE))
                                {
                                    throw new InteropHelpers.NativeCodeException("pausing job failed", "SetJob");
                                }
                            }
                        }
                    }
                }
            }
        }

        public void StopPausingNewJobs()
        {
            int previousCount;
            if (!Natives.Kernel32.ReleaseSemaphore(StopSemaphore, 1, out previousCount))
            {
                throw new InteropHelpers.NativeCodeException("failed to trigger stopping semaphore", "ReleaseSemaphore");
            }
        }

        #region Cleanup logic

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                // if (disposing) { /* dispose other managed objects */ }

                if (StopSemaphore != IntPtr.Zero)
                {
                    Natives.Kernel32.CloseHandle(StopSemaphore);
                    StopSemaphore = IntPtr.Zero;
                }
            }
            IsDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Spooler()
        {
            Dispose(false);
        }

        #endregion
    }
}
