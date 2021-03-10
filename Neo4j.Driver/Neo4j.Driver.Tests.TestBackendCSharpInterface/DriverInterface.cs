using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver;

namespace Neo4j_TestBackendDriverInterface
{
	public record AuthTokenObject(string principal, string credentials, string realm, string scheme, string ticket);
	public record SessionConfigObject(string database, string accessMode, List<string> bookmarks, long fetchsize);

	public class DriverObject
	{
		private IDriver DriverInterface { get; set; }
		private SessionConfigObject Config { get; set; }

		internal DriverObject(IDriver driver)
		{
			DriverInterface = driver;
		}

		public async Task CloseAsync()
		{
			await DriverInterface.CloseAsync();
		}

		private AccessMode GetAccessMode
		{
			get
			{
				if (Config.accessMode == "r")
					return AccessMode.Read;
				else
					return AccessMode.Write;
			}
		}

		private void SessionConfig(SessionConfigBuilder configBuilder)
		{
			if (!string.IsNullOrEmpty(Config.database)) configBuilder.WithDatabase(Config.database);
			if (!string.IsNullOrEmpty(Config.accessMode)) configBuilder.WithDefaultAccessMode(GetAccessMode);
			if (Config.bookmarks.Count > 0) configBuilder.WithBookmarks(Bookmark.From(Config.bookmarks.ToArray()));
			configBuilder.WithFetchSize(Config.fetchsize);
		}

		//TODO: This needs to return a SessionObject type defined in this dll wrapper as internally on the C-Bindings version it will call the c functions.
		public IAsyncSession AsyncSession(SessionConfigObject sessionConfigObj)
		{
			Config = sessionConfigObj;
			return DriverInterface.AsyncSession(SessionConfig);			
		}
	}

	public static class DriverInterface
	{
		private static string UserAgent = string.Empty;

		private static void DriverConfig(ConfigBuilder configBuilder)
		{
			if (!string.IsNullOrEmpty(UserAgent)) configBuilder.WithUserAgent(UserAgent);
		}

		public static DriverObject NewDriver(string uri, AuthTokenObject auth, string userAgent)
		{
			UserAgent = userAgent;
			var authToken = AuthTokens.Custom(auth.principal, auth.credentials, auth.realm, auth.scheme);
			return new DriverObject(GraphDatabase.Driver(uri, authToken, DriverConfig));			
		}		
	}
}
