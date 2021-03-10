#pragma once

#include "AuthTokenObject.h"
#include "DriverObject.h"
#include "cbindings.h"

using namespace System;

namespace Neo4j_TestBackendDriverInterface
{
	public ref class DriverInterface
	{
	private:
		static String^ _userAgent;

		DriverInterface() {};
		~DriverInterface() {};

	public:

		static DriverObject^ NewDriver(String^ uri, AuthTokenObject auth, String^ userAgent)
		{
			_userAgent = userAgent;

			neo4j_driverconfig driverConfig;
			neo4j_error* err = NULL;
			neo4j_handle driverHandle = 0;

			driverConfig.uri = "neo4j://localhost";

			neo4j_driver_create(&driverConfig, &driverHandle, &err);
			
			//TODO: How to handle errors and report across the dll boundary. Possibly an exception is ok as this is .net calling into this method.
			
			return gcnew DriverObject(driverHandle);

		}
	};
}
