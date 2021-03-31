﻿using System;
using System.Threading.Tasks;

using Neo4j_TestBackendDriverInterface;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionClose : IProtocolObject
    {
        public SessionCloseType data { get; set; } = new SessionCloseType();
       
        public class SessionCloseType
        {
            public string sessionId { get; set; }
        }

        public override async Task Process()
        {   
            SessionInterface session = ((NewSession)ObjManager.GetObject(data.sessionId)).Session;
            await session.CloseAsync();
        }

        public override string Respond()
        {  
            return new ProtocolResponse("Session", uniqueId).Encode();
        }
    }
}
