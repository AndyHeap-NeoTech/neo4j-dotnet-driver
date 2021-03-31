using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver;

namespace Neo4j_TestBackendDriverInterface
{
	public class TransactionInterface
	{
		public IAsyncTransaction Transaction { get; }


		internal TransactionInterface(IAsyncTransaction tx)
		{
			Transaction = tx;
		}

		public async Task CommitAsync()
		{
			await Transaction.CommitAsync().ConfigureAwait(false);			
		}

		public async Task RollbackAsync()
		{
			await Transaction.RollbackAsync().ConfigureAwait(false);
		}

		public async Task<ResultCursorInterface> RunAsync(string query, IDictionary<string, object> parameters)
		{	
			return new ResultCursorInterface(await Transaction.RunAsync(query, parameters).ConfigureAwait(false));
		}
	}
}
