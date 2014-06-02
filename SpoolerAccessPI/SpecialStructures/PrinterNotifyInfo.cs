using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SpoolerAccessPI.SpecialStructures
{
    class PrinterNotifyInfo
    {
        public class Data
        {
            public ushort Type { get; private set; }
            public ushort Field { get; private set; }
            public uint Reserved { get; private set; }
            public uint ID { get; private set; }

            public uint IntegerData0 { get; private set; }
            public uint IntegerData1 { get; private set; }

            public uint ConstantBuffer { get; private set; }
            public IntPtr BinaryBuffer { get; private set; }

            private static IntPtr Offset(IntPtr beginning, string fieldName)
            {
                return IntPtr.Add(beginning, Marshal.OffsetOf(typeof(Natives.Structures.PRINTER_NOTIFY_INFO_DATA), fieldName).ToInt32());
            }

            private static IntPtr IntegerDataOffset(IntPtr beginning, string fieldName)
            {
                var totalOffset
                    = Marshal.OffsetOf(typeof(Natives.Structures.PRINTER_NOTIFY_INFO_DATA), "NotifyData").ToInt32()
                    + Marshal.OffsetOf(typeof(Natives.Structures.PRINTER_NOTIFY_INFO_DATA_NotifyData_IntegerData), fieldName).ToInt32()
                ;
                return IntPtr.Add(beginning, totalOffset);
            }

            private static IntPtr DataOffset(IntPtr beginning, string fieldName)
            {
                var totalOffset
                    = Marshal.OffsetOf(typeof(Natives.Structures.PRINTER_NOTIFY_INFO_DATA), "NotifyData").ToInt32()
                    + Marshal.OffsetOf(typeof(Natives.Structures.PRINTER_NOTIFY_INFO_DATA_NotifyData_Data), fieldName).ToInt32()
                ;
                return IntPtr.Add(beginning, totalOffset);
            }

            public static PrinterNotifyInfo.Data Deserialize(IntPtr beginning)
            {
                var ret = new PrinterNotifyInfo.Data();
                ret.Type = (ushort)Marshal.ReadInt16(Offset(beginning, "Type"));
                ret.Field = (ushort)Marshal.ReadInt16(Offset(beginning, "Field"));
                ret.Reserved = (uint)Marshal.ReadInt32(Offset(beginning, "Reserved"));
                ret.ID = (uint)Marshal.ReadInt32(Offset(beginning, "ID"));

                ret.IntegerData0 = (uint)Marshal.ReadInt32(IntegerDataOffset(beginning, "IntegerData0"));
                ret.IntegerData1 = (uint)Marshal.ReadInt32(IntegerDataOffset(beginning, "IntegerData1"));

                ret.ConstantBuffer = (uint)Marshal.ReadInt32(DataOffset(beginning, "ConstantBuffer"));
                ret.BinaryBuffer = Marshal.ReadIntPtr(DataOffset(beginning, "BinaryBuffer"));

                return ret;
            }
        }

        public uint Version { get; private set; }
        public uint Flags { get; private set; }
        public List<PrinterNotifyInfo.Data> DataList { get; private set; }

        private static IntPtr Offset(IntPtr beginning, string fieldName)
        {
            return IntPtr.Add(beginning, Marshal.OffsetOf(typeof(Natives.Structures.PRINTER_NOTIFY_INFO), fieldName).ToInt32());
        }

        private static IntPtr NthDataOffset(IntPtr beginning, int n)
        {
            var offset = Marshal.OffsetOf(typeof(Natives.Structures.PRINTER_NOTIFY_INFO), "Data").ToInt32() + n * Marshal.SizeOf(typeof(Natives.Structures.PRINTER_NOTIFY_INFO_DATA));
            return IntPtr.Add(beginning, offset);
        }

        public static PrinterNotifyInfo Deserialize(IntPtr beginning)
        {
            var ret = new PrinterNotifyInfo();
            ret.Version = (uint)Marshal.ReadInt32(Offset(beginning, "Version"));
            ret.Flags = (uint)Marshal.ReadInt32(Offset(beginning, "Flags"));
            var count = (uint)Marshal.ReadInt32(Offset(beginning, "Count"));
            ret.DataList = new List<Data>();

            for (int i = 0; i < count; ++i)
            {
                ret.DataList.Add(PrinterNotifyInfo.Data.Deserialize(NthDataOffset(beginning, i)));
            }

            return ret;
        }
    }
}
