#include "pch.h"
#include "Unmanaged.h"

//TODO: REMOVE Unmanaged class TEST ONLY
int Unmanaged::HelloWorld(void)
{
	return MessageBox(NULL, L"HelloWorld", L"Unmanaged", MB_OK);
}