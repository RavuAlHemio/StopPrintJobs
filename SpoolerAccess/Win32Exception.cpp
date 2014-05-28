// This is the main DLL file.

#include "stdafx.h"

#include "Win32Exception.h"

namespace SpoolerAccess {

	Win32Exception::Win32Exception(String^ message, String^ nativeFunction)
		: SystemException(message), m_ErrorCode(GetLastError()), m_NativeFunction(nativeFunction)
	{

	}

	int Win32Exception::ErrorCode::get()
	{
		return m_ErrorCode;
	}

	String^ Win32Exception::NativeFunction::get()
	{
		return m_NativeFunction;
	}
}
