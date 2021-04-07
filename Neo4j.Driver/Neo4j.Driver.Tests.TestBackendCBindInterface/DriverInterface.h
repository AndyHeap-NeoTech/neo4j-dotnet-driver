#pragma once

#include <iostream>
#include <msclr\marshal.h>
#include "cbindings.h"
#include "DriverConfigObject.h"
#include "SessionConfigObject.h"
#include "SessionInterface.h"

using namespace System::Runtime::InteropServices;
using namespace System::IO;


public ref class DriverInterface
{
private:
	neo4j_handle _driverInterface;
	SessionConfigObject^ _sessionConfig;
	IntPtr _uriString;

public:
	DriverInterface(DriverConfigObject newConfig)
	{	
		//TODO: Add process config method, should throw on unsupported elements (callbacks etc) if set to true.

		_uriString = Marshal::StringToHGlobalAnsi(newConfig._uri);
		neo4j_error* err = NULL;
		neo4j_driverconfig driverConfig;
		neo4j_handle driverInterface = 0;
		
		driverConfig.uri = (char*)_uriString.ToPointer();
		
		if (!neo4j_driver_create(&driverConfig, &driverInterface, &err))
		{
			String^ errorString = gcnew String(err->desc);
			Console::WriteLine(errorString);

			Exception^ ex = gcnew Exception(errorString);
			neo4j_err_free(&err);
			
			throw (Exception^)ex;
		}

		_driverInterface = driverInterface;
	};

	~DriverInterface() 
	{
		Marshal::FreeHGlobal(_uriString);
	}

	void CloseAsync()
	{
		neo4j_driver_destroy(_driverInterface);
	}

	SessionInterface^ AsyncSession(SessionConfigObject sessionConfigObj)
	{
		return gcnew SessionInterface();
	}

	bool SupportsMultiDbAsync()
	{
		throw gcnew NotImplementedException("SupportsMultiDbAsync not implemented yet in CBindings");
	}

	void VerifyConnectivityAsync()
	{
		throw gcnew NotImplementedException("VerifyConectivityAsyc not implemented yet in CBindings");
	}	
};


