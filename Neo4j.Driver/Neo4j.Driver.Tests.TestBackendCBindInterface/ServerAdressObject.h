#pragma once

using namespace System;


public ref struct ServerAddressObject
{
public:
	String^ _host;
	String^ _port;

public:
	ServerAddressObject(String^ host, String^ port)
	{
		_host = host;
		_port = port;
	}

	~ServerAddressObject() {}
};
