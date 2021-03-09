using System;
using Neo4j.Driver;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

using Neo4j.Driver.Tests.TestBackendDriverInterface;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class NewDriver : IProtocolObject
    {
        public NewDriverType data { get; set; } = new NewDriverType();
        [JsonIgnore]
        public DriverObject Driver { get; set; }

        public class NewDriverType
        {
            public string uri { get; set; }
            public AuthorizationToken authorizationToken { get; set; } = new AuthorizationToken();
            public string userAgent { get; set; }
        }

        void DriverConfig(ConfigBuilder configBuilder)
        {
            if (!string.IsNullOrEmpty(data.userAgent)) configBuilder.WithUserAgent(data.userAgent);
        }

        public override async Task Process()
        {
			var authTokenData = data.authorizationToken.data;
			var authTokenObject = new AuthTokenObject(authTokenData.principal, authTokenData.credentials, authTokenData.realm, authTokenData.scheme, authTokenData.ticket);
			Driver = DriverInterface.NewDriver(data.uri, authTokenObject, data.userAgent);

			await Task.CompletedTask;
		}

        public override string Respond()
        {
            return new ProtocolResponse("Driver", uniqueId).Encode();
        }
    }
}
