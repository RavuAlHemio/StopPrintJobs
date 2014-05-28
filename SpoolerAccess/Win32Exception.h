// Win32Exception.h

#pragma once

using namespace System;

namespace SpoolerAccess {

	public ref class Win32Exception : public SystemException
	{
	private:
		int m_ErrorCode;
		String^ m_NativeFunction;

	public:
		Win32Exception(String^ message, String^ nativeFunction);

		property int ErrorCode
		{
			int get();
		}

		property String^ NativeFunction
		{
			String^ get();
		}
	};

}
