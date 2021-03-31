using System;
using System.Threading.Tasks;

using Neo4j_TestBackendDriverInterface;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionLastBookmarks : IProtocolObject
    {
        string[] Bookmarks { get; set; }
        public SessionLastBookmarksType data { get; set; } = new SessionLastBookmarksType();

        public class SessionLastBookmarksType
        {
            public string sessionId { get; set; }
        }

        public override async Task Process()
        {
            SessionInterface session = ((NewSession)ObjManager.GetObject(data.sessionId)).Session;
			Bookmarks = session.LastBookmark.values;
            await Task.CompletedTask;
        }

        public override string Respond()
        {
            return new ProtocolResponse("Bookmarks", new { bookmarks = Bookmarks }).Encode();
            
        }
    }
}
