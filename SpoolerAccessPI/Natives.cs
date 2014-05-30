using System;
using System.Runtime.InteropServices;

namespace SpoolerAccessPI
{
	public static class Natives
    {
		public static class Constants
		{
			public const uint ERROR_INSUFFICIENT_BUFFER = 122;

			public const uint INFINITE = 0xffffffff;

			public const uint PRINTER_CHANGE_ADD_JOB = 0x00000100;

			public const uint PRINTER_ENUM_LOCAL = 0x00000002;
			public const uint PRINTER_ENUM_SHARED = 0x00000020;

			public const ushort PRINTER_NOTIFY_FIELD_STATUS = 0x12;

			public const ushort JOB_NOTIFY_TYPE = 0x01;

			public const uint JOB_CONTROL_PAUSE = 1;

			public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
		}

		public static class Kernel32
		{
			[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern bool CloseHandle(IntPtr handle);

			[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern uint WaitForMultipleObjects(uint howMany, IntPtr[] handles, bool waitForAll, uint milliseconds);

			[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern IntPtr CreateSemaphore(IntPtr securityAttributes, int initialCount, int maxCount, string name);

			[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern bool ReleaseSemaphore(IntPtr semaphoreHandle, int releaseCount, out int previousCount);
		}

		public static class Winspool
		{
			[DllImport("winspool.drv", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern bool EnumPrinters(uint flags, string name, uint level, IntPtr buffer, uint bufferSize, out uint bytesNeeded, out uint printersStored);

			[DllImport("winspool.drv", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern bool OpenPrinter2(string printerName, out IntPtr printerHandle, IntPtr printerDefaults, IntPtr printerOptions);

			[DllImport("winspool.drv", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern bool ClosePrinter(IntPtr printerHandle);

			[DllImport("winspool.drv", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern IntPtr FindFirstPrinterChangeNotification(IntPtr printerHandle, uint filter, uint options, IntPtr printerNotifyOptions);

			[DllImport("winspool.drv", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern bool FindNextPrinterChangeNotification(IntPtr notifHandle, out uint whatChanged, IntPtr printerNotifyOptions, out IntPtr printerNotifyInfo);

			[DllImport("winspool.drv", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern bool FreePrinterNotifyInfo(IntPtr printerNotifyInfo);

			[DllImport("winspool.drv", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern bool FindClosePrinterChangeNotification(IntPtr notifHandle);

			[DllImport("winspool.drv", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
			public static extern bool SetJob(IntPtr printerHandle, uint jobID, uint jobInfoLevel, IntPtr jobInfo, uint command);
		}
    }
}
