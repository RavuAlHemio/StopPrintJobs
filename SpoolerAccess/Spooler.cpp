// This is the main DLL file.

#include "stdafx.h"

#include "Deleter.h"
#include "Spooler.h"
#include "Win32Exception.h"

template class ArrayDeleter < unsigned char > ;

namespace SpoolerAccess {
	
	Spooler::Spooler()
		: m_stopSemaphore(nullptr)
	{
		m_stopSemaphore = CreateSemaphoreW(nullptr, 0, 1, nullptr);
		if (m_stopSemaphore == nullptr)
		{
			throw gcnew SpoolerAccess::Win32Exception("failed to create stopping semaphore", "CreateSemaphore");
		}
	}

	Spooler::~Spooler()
	{
		if (m_stopSemaphore != nullptr)
		{
			if (!CloseHandle(m_stopSemaphore))
			{
				throw gcnew SpoolerAccess::Win32Exception("failed to release stopping semaphore", "CloseHandle");
			}
		}
	}

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

		auto printerStructures = reinterpret_cast<PRINTER_INFO_4W *>(printerStructureBytes);
		unsigned int i;
		auto ret = gcnew List<String^>();
		for (i = 0; i < itemsReturned; ++i)
		{
			ret->Add(gcnew String(printerStructures[i].pPrinterName));
		}

		return ret;
	}

	void Spooler::PauseNewJobsProc(List<String^>^ printersToPause)
	{
		if (printersToPause == nullptr || printersToPause->Count == 0)
		{
			return;
		}

		// fetch handles to the printers
		std::vector<HANDLE> printers;
		StdCallDeleterVector<HANDLE, ClosePrinter > printerDeleters;
		
		for each (String^ printerNameString in printersToPause)
		{
			pin_ptr<const wchar_t> printerName = PtrToStringChars(printerNameString);
			
			HANDLE printerHandle;
			if (!OpenPrinter2W(printerName, &printerHandle, nullptr, nullptr) || printerHandle == INVALID_HANDLE_VALUE)
			{
				throw gcnew SpoolerAccess::Win32Exception("failed to get open printer " + printerNameString, "OpenPrinter2");
			}
			printers.push_back(printerHandle);
			printerDeleters.push_back(printerHandle);
		}

		// prepare notification options
		WORD fields[] = { PRINTER_NOTIFY_FIELD_STATUS };
		PRINTER_NOTIFY_OPTIONS_TYPE notifyOptionsType;
		notifyOptionsType.Type = JOB_NOTIFY_TYPE;
		notifyOptionsType.Reserved0 = 0;
		notifyOptionsType.Reserved1 = 0;
		notifyOptionsType.Reserved2 = 0;
		notifyOptionsType.Count = 1;
		notifyOptionsType.pFields = fields;
		PRINTER_NOTIFY_OPTIONS notifyOptions;
		notifyOptions.Version = 2;
		notifyOptions.Flags = 0;
		notifyOptions.Count = 1;
		notifyOptions.pTypes = &notifyOptionsType;

		// fetch notification handles
		std::vector<HANDLE> semaphoreAndNotifications;
		StdCallDeleterVector<HANDLE, FindClosePrinterChangeNotification> notificationDeleters;

		semaphoreAndNotifications.push_back(m_stopSemaphore);

		for (auto&& printer : printers)
		{
			HANDLE notif = FindFirstPrinterChangeNotification(printer, PRINTER_CHANGE_ADD_JOB, 0, &notifyOptions);
			if (notif == INVALID_HANDLE_VALUE)
			{
				throw gcnew SpoolerAccess::Win32Exception("failed to subscribe to notifications for a printer", "FindFirstPrinterChangeNotification");
			}
			semaphoreAndNotifications.push_back(notif);
			notificationDeleters.push_back(notif);
		}

		// and we wait and we wonder
		for (;;)
		{
			DWORD waitResult = WaitForMultipleObjects(semaphoreAndNotifications.size(), &semaphoreAndNotifications[0], FALSE, INFINITE);
			if (waitResult >= semaphoreAndNotifications.size())
			{
				throw gcnew SpoolerAccess::Win32Exception("waiting for printers failed", "WaitForMultipleObjects");
			}
			else if (waitResult == 0)
			{
				// stopping semaphore triggered
				break;
			}

			// notification triggered

			HANDLE triggeredNotif = semaphoreAndNotifications[waitResult];
			DWORD whatChanged;
			PRINTER_NOTIFY_INFO *notifyInfo;
			if (!FindNextPrinterChangeNotification(triggeredNotif, &whatChanged, &notifyOptions, reinterpret_cast<LPVOID*>(&notifyInfo)))
			{
				throw gcnew SpoolerAccess::Win32Exception("fetching change notification failed", "FindNextPrinterChangeNotification");
			}
			StdCallDeleter<PRINTER_NOTIFY_INFO*, FreePrinterNotifyInfo> notifyInfoDeleter(notifyInfo);

			// alright, what's the job's ID?
			if (notifyInfo->Count == 0)
			{
				throw gcnew SpoolerAccess::Win32Exception("add-job notification with no data?!", "FindNextPrinterChangeNotification");
			}

			PRINTER_NOTIFY_INFO_DATA *statusData = &(notifyInfo->aData[0]);
			if (statusData->Type != JOB_NOTIFY_TYPE)
			{
				throw gcnew SpoolerAccess::Win32Exception("add-job notification with no job-related data?!", "FindNextPrinterChangeNotification");
			}

			// pause it
			// (need to subtract 1 because 0 is the stopping semaphore)
			if (!SetJobW(printers[waitResult-1], statusData->Id, 0, nullptr, JOB_CONTROL_PAUSE))
			{
				throw gcnew SpoolerAccess::Win32Exception("pausing job failed", "SetJob");
			}
		}
	}

	void Spooler::StopPausingNewJobs()
	{
		if (!ReleaseSemaphore(m_stopSemaphore, 1, nullptr))
		{
			throw gcnew SpoolerAccess::Win32Exception("failed to trigger stopping semaphore", "ReleaseSemaphore");
		}
	}
}
