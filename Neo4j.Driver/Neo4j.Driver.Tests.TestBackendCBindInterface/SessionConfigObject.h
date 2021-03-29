#pragma once
using namespace System;
using namespace System::Collections::Generic;

public ref class SessionConfigObject
{
public:
	String^ _database;
	String^ _accessMode;
	List<String^> _bookmarks;
	long _fetchSize;

public:
	SessionConfigObject(String^ database, String^ accessMode, List<String^>^ bookmarks, long fetchSize)
	{
		_database = database;
		_accessMode = accessMode;
		_bookmarks.AddRange(bookmarks);
		_fetchSize = fetchSize;
	}

	~SessionConfigObject() {}
};



