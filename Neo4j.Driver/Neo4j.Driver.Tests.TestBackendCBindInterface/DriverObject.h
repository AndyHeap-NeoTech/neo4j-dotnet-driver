#pragma once

#include "cbindings.h"

public ref class DriverObject
{
private:
	neo4j_handle _driverInterface;
	
internal:
	//TODO: 
	DriverObject(neo4j_handle driverHandle) 
	{
		_driverInterface = driverHandle;
	};

	~DriverObject() {};

public:

	void CloseAsync()
	{
		neo4j_driver_destroy(_driverInterface);
	}

	
};