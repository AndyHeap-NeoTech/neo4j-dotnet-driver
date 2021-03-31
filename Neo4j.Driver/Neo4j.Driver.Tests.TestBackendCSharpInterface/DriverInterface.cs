using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver;
using System.Linq;

namespace Neo4j_TestBackendDriverInterface
{
	public record AuthTokenObject(string principal, string credentials, string realm, string scheme, string ticket);	
	public record ServerAddressObject(string host, string port);


	public class DriverConfigObject
	{
		public AuthTokenObject AuthTokenData { get; set; }
		public string Uri { get; set; } = string.Empty;
		public string UserAgent { get; set; } = string.Empty;
		public bool ResolverRegistered { get; set; } = false;
		public int ConnectionTimeoutMS { get; set; } = -1;
		public Func<Uri, List<ServerAddressObject>> AddressResolver { get; set; }
	}


	public class DriverInterface
	{
		private IDriver Driver { get; set; }
		private SessionConfigObject SessionConfig { get; set; }
		private DriverConfigObject DriverConfig { get; set; }


		public DriverInterface(DriverConfigObject driverConfig)
		{
			DriverConfig = driverConfig;
			var authToken = AuthTokens.Custom(DriverConfig.AuthTokenData.principal, 
											  DriverConfig.AuthTokenData.credentials, 
											  DriverConfig.AuthTokenData.realm, 
											  DriverConfig.AuthTokenData.scheme);

			Driver = GraphDatabase.Driver(DriverConfig.Uri, authToken, BuildDriverConfig);
		}

		public async Task CloseAsync()
		{
			await Driver.CloseAsync();
		}

		public SessionInterface AsyncSession(SessionConfigObject sessionConfigObj)
		{
			SessionConfig = sessionConfigObj;
			return new SessionInterface(Driver.AsyncSession(BuildSessionConfig));
		}

		public async Task<bool> SupportsMultiDbAsync()
		{
			return await Driver.SupportsMultiDbAsync();
		}

		public async Task VerifyConnectivityAsync()
		{
			await Driver.VerifyConnectivityAsync();
		}

		private AccessMode GetAccessMode
		{
			get
			{
				if (SessionConfig.accessMode == "r")
					return AccessMode.Read;
				else
					return AccessMode.Write;
			}
		}

		private void BuildDriverConfig(ConfigBuilder configBuilder)
		{
			if (!string.IsNullOrEmpty(DriverConfig.UserAgent)) configBuilder.WithUserAgent(DriverConfig.UserAgent);

			if (DriverConfig.ResolverRegistered) configBuilder.WithResolver(new ListAddressResolver(DriverConfig));

			if (DriverConfig.ConnectionTimeoutMS > 0) configBuilder.WithConnectionTimeout(TimeSpan.FromMilliseconds(DriverConfig.ConnectionTimeoutMS));
		}

		private void BuildSessionConfig(SessionConfigBuilder configBuilder)
		{
			if (!string.IsNullOrEmpty(SessionConfig.database)) configBuilder.WithDatabase(SessionConfig.database);
			if (!string.IsNullOrEmpty(SessionConfig.accessMode)) configBuilder.WithDefaultAccessMode(GetAccessMode);
			if (SessionConfig.bookmarks.Count > 0) configBuilder.WithBookmarks(Bookmark.From(SessionConfig.bookmarks.ToArray()));
			configBuilder.WithFetchSize(SessionConfig.fetchsize);
		}		
	}


	internal class ListAddressResolver : IServerAddressResolver
	{	
		private Func<Uri, List<ServerAddressObject>> Callback { get; set; }
		Uri Uri { get; }


		public ListAddressResolver(DriverConfigObject config)
		{	
			Uri = new Uri(config.Uri);
			Callback = config.AddressResolver;
		}

		public ISet<ServerAddress> Resolve(ServerAddress address)
		{
			var callbackResult = Callback(Uri);
			return new HashSet<ServerAddress>(callbackResult.Select(x => { return ServerAddress.From(x.host, Convert.ToInt32(x.port)); }));			
		}
	}

}
