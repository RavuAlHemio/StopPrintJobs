// SpoolerAccess.h

#pragma once

using namespace System;
using namespace System::Collections::Generic;

namespace SpoolerAccess {

	public ref class Spooler
	{
	public:
		static List<String^>^ EnumLocalPrinters(bool sharedOnly);
		static void PauseNewJobsProc(List<String^>^ printersToPause);

		// TODO: Add your methods for this class here.
	};
}
