using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Neo4j_TestBackendDriverInterface;


namespace Neo4j.Driver.Tests.TestBackend
{
	internal class TransactionWrapper
	{
		public TransactionInterface Transaction { get; private set; }
		private Func<ResultCursorInterface, Task<string>> ResultHandler;

		public TransactionWrapper(TransactionInterface transaction, Func<ResultCursorInterface, Task<string>>resultHandler)
		{
			Transaction = transaction;
			ResultHandler = resultHandler;
		}

		public async Task<string> ProcessResults(ResultCursorInterface cursor)
		{
			return await ResultHandler(cursor);
		}

	}


	internal class TransactionManager
	{
		private Dictionary<string, TransactionWrapper> Transactions { get; set; } = new Dictionary<string, TransactionWrapper>();

		public string AddTransaction(TransactionWrapper transation)
		{
			var key = ProtocolObjectManager.GenerateUniqueIdString();
			Transactions.Add(key, transation);
			return key;
		}

		public void RemoveTransaction(string key)
		{
			Transactions.Remove(key);
		}

		public TransactionWrapper FindTransaction(string key)
		{
			return Transactions[key];
		}

	}
}
