using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4j_TestBackendDriverInterface
{
	public record SessionConfigObject(string database, string accessMode, List<string> bookmarks, long fetchsize);
	public record TransactionConfigObject(int timeout, IDictionary<string, object> txMeta);
	public record BookmarkObject(string[] values);

	public class SessionInterface
	{
		private IAsyncSession Session { get; set; }
		private TransactionConfigObject TxConfig { get; set; }


		public BookmarkObject LastBookmark 
		{ 
			get
			{
				return new BookmarkObject(values: Session.LastBookmark is null 
												  ? Array.Empty<string>() 
												  : Session.LastBookmark.Values); 
			}			
		}

		internal SessionInterface(IAsyncSession session)
		{
			Session = session;
		}

		public async Task<ResultCursorInterface> RunAsync(string cypher, Dictionary<string, object> parameters, TransactionConfigObject txConfig)
		{
			TxConfig = txConfig;
			return new ResultCursorInterface(await Session.RunAsync(cypher, parameters, TransactionConfig));
		}

		public async Task CloseAsync()
		{
			await Session.CloseAsync().ConfigureAwait(false);
		}

		public async Task<TransactionInterface> BeginTransactionAsync(TransactionConfigObject txConfig)
		{
			TxConfig = txConfig;
			var tx = await Session.BeginTransactionAsync(TransactionConfig).ConfigureAwait(false);
			return new TransactionInterface(tx);
		}

		public async Task ReadTransactionAsync(Func<TransactionInterface, Task> work, TransactionConfigObject txConfig)
		{
			TxConfig = txConfig;

			await Session.ReadTransactionAsync(async tx =>
			{
				var txInterface = new TransactionInterface(tx);
				await work(txInterface);
			}, TransactionConfig);
		}

		public async Task WriteTransactionAsync(Func<TransactionInterface, Task> work, TransactionConfigObject txConfig)
		{
			TxConfig = txConfig;

			await Session.WriteTransactionAsync(async tx =>
			{
				var txInterface = new TransactionInterface(tx);
				await work(txInterface);
			}, TransactionConfig);
		}


		private void TransactionConfig(TransactionConfigBuilder configBuilder)
		{
			if (TxConfig.timeout != -1)
			{
				var time = TimeSpan.FromMilliseconds(TxConfig.timeout);
				configBuilder.WithTimeout(time);
			}

			if (!(TxConfig.txMeta is null)) configBuilder.WithMetadata(TxConfig.txMeta);
		}
	}
}
