// This is the main DLL file.

#include "stdafx.h"

#include "Deleter.h"
#include "Spooler.h"
#include "Win32Exception.h"

template class ArrayDeleter < unsigned char > ;

namespace SpoolerAccess {

	List<String^>^ Spooler::EnumLocalPrinters(bool sharedOnly)
	{
		DWORD searchFlags = PRINTER_ENUM_LOCAL;
		DWORD bytesNeeded, bytesReturned, itemsReturned;

		if (sharedOnly) {
			searchFlags |= PRINTER_ENUM_SHARED;
		}

		// see how much space we'll need
		if (!EnumPrintersW(searchFlags, nullptr, 4, nullptr, 0, &bytesNeeded, &itemsReturned))
		{
			if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
			{
				throw gcnew SpoolerAccess::Win32Exception("failed to get space requirement for printer list", "EnumPrinters");
			}
		}

		// allocate that
		unsigned char *printerStructureBytes = new unsigned char[bytesNeeded];
		ArrayDeleter<unsigned char> deletePrinterStructures(printerStructureBytes);

		// fetch again
		if (!EnumPrintersW(searchFlags, nullptr, 4, printerStructureBytes, bytesNeeded, &bytesReturned, &itemsReturned))
		{
			throw gcnew SpoolerAccess::Win32Exception("failed to get printer list", "EnumPrinters");
		}

		PRINTER_INFO_4W *printerStructures = reinterpret_cast<PRINTER_INFO_4W *>(printerStructureBytes);
		int i;
		List<String^>^ ret = gcnew List<String^>();
		for (i = 0; i < itemsReturned; ++i)
		{
			ret->Add(gcnew String(printerStructures[i].pPrinterName));
		}

		return ret;
	}

	void Spooler::PauseNewJobsProc(List<String^>^ printersToPause)
	{
		// TODO
	}
}
