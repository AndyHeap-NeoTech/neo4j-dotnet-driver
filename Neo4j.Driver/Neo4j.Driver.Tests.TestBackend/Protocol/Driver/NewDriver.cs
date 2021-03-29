using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

using Neo4j_TestBackendDriverInterface;


namespace Neo4j.Driver.Tests.TestBackend
{       
    internal class NewDriver : IProtocolObject
    {
        public NewDriverType data { get; set; } = new NewDriverType();
        [JsonIgnore]
        public DriverInterface Driver { get; set; }
		[JsonIgnore]
		private Controller Control { get; set; }

		public class NewDriverType
        {
            public string uri { get; set; }
            public AuthorizationToken authorizationToken { get; set; } = new AuthorizationToken();
            public string userAgent { get; set; }
			public bool resolverRegistered { get; set; } = false;
			public bool domainNameResolverRegistered { get; set; } = false;
			public int connectionTimeoutMs { get; set; } = -1;
		}

        public override async Task Process(Controller controller)
		{
			Control = controller;
			var authTokenData = data.authorizationToken.data;
			var authTokenObject = new AuthTokenObject(authTokenData.principal, authTokenData.credentials, authTokenData.realm, authTokenData.scheme, authTokenData.ticket);
			var driverConfig = new DriverConfigObject()
			{
				AuthTokenData = authTokenObject,
				Uri = data.uri,
				UserAgent = data.userAgent,
				ResolverRegistered = data.resolverRegistered,
				ConnectionTimeoutMS = data.connectionTimeoutMs,
				AddressResolver = AddressResolverCallback
			};

			Driver = new DriverInterface(driverConfig);
			await Task.CompletedTask;
		}

        public override string Respond()
        {
            return new ProtocolResponse("Driver", uniqueId).Encode();
        }

		private List<ServerAddressObject> AddressResolverCallback(Uri address)
		{
			string errorMessage = "A ResolverResolutionCompleted request is expected straight after a ResolverResolutionRequired reponse is sent";
			var response = new ProtocolResponse("ResolverResolutionRequired",
												new
												{
													id = ProtocolObjectManager.GenerateUniqueIdString(),
													address = address.Host + ":" + address.Port
												})
												.Encode();

			//Send the ResolverResolutionRequired response
			Control.SendResponse(response).ConfigureAwait(false);

			//Read the ResolverResolutionCompleted request, throw if another type of request has come in
			var result = Control.TryConsumeStreamObjectOfType<ResolverResolutionCompleted>().Result;
			if (result is null)
				throw new NotSupportedException(errorMessage);

			//Return a IServerAddressResolver instance thats Resolve method uses the addresses in the ResolverResolutionoCompleted request.
			return new List<ServerAddressObject>(result
											  .data
											  .addresses
											  .Select(x =>
											  {
												  string[] split = x.Split(':');
												  return new ServerAddressObject(split[0], split[1]);
											  }));			
		}
	}
}