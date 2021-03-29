#pragma once

#include "AuthTokenObject.h"
#include "ServerAdressObject.h"

using namespace System;
using namespace System::Collections::Generic;


public delegate List<ServerAddressObject^> AddressResolverCallback(String^ uri);

public ref class DriverConfigObject
{
public:

	AuthTokenObject^ _authToken;
	String^ _uri;
	String^ _userAgent;
	bool _resolverRegistered;
	int _connectionTimeout;
	AddressResolverCallback^ _addressCallback;

public:
	DriverConfigObject(
		AuthTokenObject^ authToken,
		String^ uri,
		String^ userAgent,
		bool resolverRegistered,
		int connectionTimeout,
		AddressResolverCallback^ addressCallback)
	{
		_authToken = authToken;
		_uri = uri;
		_userAgent = userAgent;
		_resolverRegistered = resolverRegistered;
		_connectionTimeout = connectionTimeout;
		_addressCallback = addressCallback;
	}

private:
	DriverConfigObject() {}
};

