#pragma once

template<typename T>
class ArrayDeleter
{
private:
	T *m_array;
public:
	ArrayDeleter(T* arr)
		: m_array(arr)
	{
	}

	~ArrayDeleter()
	{
		delete[] m_array;
	}
};
