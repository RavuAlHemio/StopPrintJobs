// SpoolerAccess.h

#pragma once

using namespace System;
using namespace System::Collections::Generic;

namespace SpoolerAccess {

	public ref class Spooler
	{
	private:
		HANDLE m_stopSemaphore;

	public:
		Spooler();
		~Spooler();

		/// Returns a list of the names of all local printers.
		static List<String^>^ EnumLocalPrinters(bool sharedOnly);

		/// Pauses incoming jobs on the printers whose names are passed as the first argument.
		void PauseNewJobsProc(List<String^>^ printersToPause);

		/// Stops pausing new jobs, making PauseNewJobsProc return. (Call this from another
		/// thread!)
		void StopPausingNewJobs();
	};
}
