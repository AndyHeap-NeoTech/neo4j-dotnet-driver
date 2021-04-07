#pragma once

using namespace System;

public ref class TestBlackList
{
private:
	TestBlackList() {}
	~TestBlackList() {}

public:
	static bool FindTest(String^ testName, String^ reason)
	{
		return false;
	}
};

