namespace StockSharp.Algo
{
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Messages;

	class LevelQuotes : IEnumerable<ExecutionMessage>
	{
		private readonly List<ExecutionMessage> _quotes = new List<ExecutionMessage>();
		private readonly Dictionary<long, ExecutionMessage> _quotesByTrId = new Dictionary<long, ExecutionMessage>();

		public int Count => _quotes.Count;

		public ExecutionMessage this[int i]
		{
			get => _quotes[i];
			set
			{
				var prev = _quotes[i];

				if (prev.TransactionId != 0)
					_quotesByTrId.Remove(prev.TransactionId);

				_quotes[i] = value;

				if (value.TransactionId != 0)
					_quotesByTrId[value.TransactionId] = value;
			}
		}

		public ExecutionMessage TryGetByTransactionId(long transactionId) => _quotesByTrId.TryGetValue(transactionId);

		public void Add(ExecutionMessage quote)
		{
			if (quote.TransactionId != 0)
				_quotesByTrId[quote.TransactionId] = quote;

			_quotes.Add(quote);
		}

		public void RemoveAt(int index, ExecutionMessage quote = null)
		{
			if (quote == null)
				quote = _quotes[index];

			_quotes.RemoveAt(index);

			if (quote.TransactionId != 0)
				_quotesByTrId.Remove(quote.TransactionId);
		}

		public void Remove(ExecutionMessage quote) => RemoveAt(_quotes.IndexOf(quote), quote);

		public IEnumerator<ExecutionMessage> GetEnumerator() => _quotes.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}