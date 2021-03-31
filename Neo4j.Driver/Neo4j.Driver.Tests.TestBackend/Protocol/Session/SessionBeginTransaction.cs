using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

using Neo4j_TestBackendDriverInterface;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionBeginTransaction : IProtocolObject
    {
        public SessionBeginTransactionType data { get; set; } = new SessionBeginTransactionType();
        [JsonIgnore]
        public string TransactionId { get; set; }

		public class SessionBeginTransactionType
		{
			public string sessionId { get; set; }

			[JsonProperty(Required = Required.AllowNull)]
			public Dictionary<string, object> txMeta { get; set; } = new Dictionary<string, object>();

			[JsonProperty(Required = Required.AllowNull)]
			public int timeout { get; set; } = -1;
        }

		public override async Task Process(Controller controller)
		{
			var txConfig = new TransactionConfigObject(data.timeout, data.txMeta);
			var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
			var transaction = await sessionContainer.Session.BeginTransactionAsync(txConfig);

			TransactionId = controller.TransactionManagager.AddTransaction(new TransactionWrapper(transaction, async cursor => 
			{	
				var result = ProtocolObjectFactory.CreateObject<SessionResult>();
				result.Results = cursor;

				return await Task.FromResult<string>(result.uniqueId);				
			}));
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", TransactionId).Encode();
        }
    }
}
