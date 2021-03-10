#pragma once
using namespace System;

public ref class AuthTokenObject
{
private:
	String^ _principle;
	String^ _credentials;
	String^ _realm;
	String^ _scheme;
	String^ _ticket;
public:
	AuthTokenObject(String^ principle, String^ credentials, String^ realm, String^ scheme, String^ ticket) :
		_principle(principle),
		_credentials(credentials),
		_realm(realm),
		_scheme(scheme),
		_ticket(ticket)
	{

	}

	~AuthTokenObject() {}
};

