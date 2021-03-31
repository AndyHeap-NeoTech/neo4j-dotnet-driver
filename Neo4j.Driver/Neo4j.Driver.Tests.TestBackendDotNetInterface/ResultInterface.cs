using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Neo4j.Driver;

namespace Neo4j_TestBackendDriverInterface
{
	public class ResultCursorInterface
	{
		internal IResultCursor CursorInterface { get; }
		public ResultRecordInterface Current => new ResultRecordInterface(CursorInterface.Current);

		internal ResultCursorInterface(IResultCursor cursor)
		{
			CursorInterface = cursor;
		}

		public async Task<string[]> KeysAsync()
		{
			return await CursorInterface.KeysAsync();
		}
		
		public async Task<ResultSummaryInterface> ConsumeAsync()
		{
			return new ResultSummaryInterface(await CursorInterface.ConsumeAsync());
		}
		
		public async Task<ResultRecordInterface> PeekAsync()
		{
			return new ResultRecordInterface(await CursorInterface.PeekAsync());
		}
		
		public Task<bool> FetchAsync()
		{
			return CursorInterface.FetchAsync();
		}
	}

	public static class ResultCursorInterfaceExtensions
	{
		public static async Task<ResultSummaryInterface> ForEachAsync(this ResultCursorInterface result, Action<ResultRecordInterface> operation)
		{
			return new ResultSummaryInterface(await result
													.CursorInterface
													.ForEachAsync(x => operation(new ResultRecordInterface(x)))
											 );
		}
	}

	public class ResultRecordInterface
	{
		private IRecord RecordInterface { get; }

		internal ResultRecordInterface(IRecord record)
		{
			RecordInterface = record;
		}

		public object this[int index] => RecordInterface.Values[Keys[index]];
		public object this[string key] => RecordInterface.Values[key];
		public IReadOnlyList<string> Keys => RecordInterface.Keys;
		public IReadOnlyDictionary<string, object> Values => RecordInterface.Values;

	}

	public class ResultSummaryInterface
	{
		private IResultSummary SummaryInterface { get; }

		internal ResultSummaryInterface(IResultSummary resultSummary)
		{
			SummaryInterface = resultSummary;
		}

		//TODO: Potentially need to add in accessors to the IResultSummary for its parameters. Will require new wrappers for any Neo4j types.
	}
}
