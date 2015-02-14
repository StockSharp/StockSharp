namespace StockSharp.Algo
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Messages;

	internal sealed class AdaptersHolder
	{
		private readonly CachedSynchronizedDictionary<IMessageProcessor, ISmartPointer> _processorPointers = new CachedSynchronizedDictionary<IMessageProcessor, ISmartPointer>();

		private readonly Action<Exception> _errorHandler;

		public IMessageAdapter TransactionAdapter { get; private set; }

		public IMessageAdapter MarketDataAdapter { get; private set; }

		public AdaptersHolder(IMessageSessionHolder session, Action<Exception> errorHandler)
		{
			_errorHandler = errorHandler;

			var transaction = session.IsTransactionEnabled;
			var marketData = session.IsMarketDataEnabled;

			if (marketData)
				MarketDataAdapter = session.CreateMarketDataAdapter();

			if (transaction)
				TransactionAdapter = session.CreateTransactionAdapter();

			var type = session.GetType();
			var displayName = type.GetDisplayName();

			if (!session.JoinInProcessors)
			{
				if (transaction)
					ApplyMessageProcessor(displayName, MessageDirections.In, true, false);

				if (marketData)
					ApplyMessageProcessor(displayName, MessageDirections.In, false, true);
			}
			else
				ApplyMessageProcessor(displayName, MessageDirections.In, transaction, marketData);

			if (!session.JoinOutProcessors)
			{
				if (transaction)
					ApplyMessageProcessor(displayName, MessageDirections.Out, true, false);

				if (marketData)
					ApplyMessageProcessor(displayName, MessageDirections.Out, false, true);
			}
			else
				ApplyMessageProcessor(displayName, MessageDirections.Out, transaction, marketData);
		}

		public void ApplyMessageProcessor(string name, MessageDirections direction, bool isTransaction, bool isMarketData, IMessageProcessor defaultProcessor = null)
		{
			var processor = new MessageProcessorPool(defaultProcessor ?? new MessageProcessor("Processor '{0}' ({1})".Put(name, direction), _errorHandler));
			ISmartPointer pointer = new SmartPointer<IMessageProcessor>(processor, p =>
			{
				p.Stop();
				_processorPointers.Remove(p);
			});

			_processorPointers[processor] = pointer;

			switch (direction)
			{
				case MessageDirections.In:
					if (isTransaction)
					{
						DecRefProcessor(TransactionAdapter, direction);

						pointer.IncRef();
						TransactionAdapter.InMessageProcessor = processor;
					}

					if (isMarketData)
					{
						DecRefProcessor(MarketDataAdapter, direction);

						pointer.IncRef();
						MarketDataAdapter.InMessageProcessor = processor;
					}

					break;
				case MessageDirections.Out:
					if (isTransaction)
					{
						DecRefProcessor(TransactionAdapter, direction);

						pointer.IncRef();
						TransactionAdapter.OutMessageProcessor = processor;
					}

					if (isMarketData)
					{
						DecRefProcessor(MarketDataAdapter, direction);

						pointer.IncRef();
						MarketDataAdapter.OutMessageProcessor = processor;
					}

					break;
				default:
					_processorPointers.Remove(processor);
					throw new ArgumentOutOfRangeException("direction");
			}
		}

		private void DecRefProcessor(IMessageProcessor processor)
		{
			var ptr = _processorPointers.TryGetValue(processor);

			if (ptr != null)
				ptr.DecRef();
		}

		private void DecRefProcessor(IMessageAdapter adapter, MessageDirections direction)
		{
			switch (direction)
			{
				case MessageDirections.In:
					if (adapter.InMessageProcessor != null)
					{
						DecRefProcessor(adapter.InMessageProcessor);
						adapter.InMessageProcessor = null;
					}
					break;

				case MessageDirections.Out:
					if (adapter.OutMessageProcessor != null)
					{
						DecRefProcessor(adapter.OutMessageProcessor);
						adapter.OutMessageProcessor = null;
					}
					break;

				default:
					throw new ArgumentOutOfRangeException("direction");
			}
		}

		public bool TryDispose(bool isTransaction)
		{
			if (isTransaction && TransactionAdapter != null)
			{
				DecRefProcessor(TransactionAdapter, MessageDirections.In);
				DecRefProcessor(TransactionAdapter, MessageDirections.Out);
			}

			if (!isTransaction && MarketDataAdapter != null)
			{
				DecRefProcessor(MarketDataAdapter, MessageDirections.In);
				DecRefProcessor(MarketDataAdapter, MessageDirections.Out);
			}

			return _processorPointers.Count == 0;
		}
	}
}