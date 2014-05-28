#pragma once

template<typename T>
class Deleter
{
private:
	T *m_what;
public:
	Deleter(T* what)
		: m_what(what)
	{
	}

	~Deleter()
	{
		delete m_what;
	}
};

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

template<typename T, BOOL (__stdcall *DelFunc)(T)>
class StdCallDeleter
{
private:
	T m_what;
public:
	StdCallDeleter(T arr)
		: m_what(arr)
	{
	}

	~StdCallDeleter()
	{
		DelFunc(m_what);
	}
};

template<typename T, BOOL(__stdcall *DelFunc)(T)>
class StdCallDeleterVector
{
private:
	std::vector<T> m_whats;
public:
	StdCallDeleterVector()
		: m_whats()
	{
	}

	StdCallDeleterVector(std::vector<T>&& whats)
		: m_whats(whats)
	{
	}

	void push_back(const T& what)
	{
		m_whats.push_back(what);
	}

	T&& operator[](size_t n)
	{
		return m_whats[n];
	}

	~StdCallDeleterVector()
	{
		for (auto&& what : m_whats)
		{
			DelFunc(what);
		}
	}
};
