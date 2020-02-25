namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.Messages;

	/// <summary>
	/// Storage based message adapter.
	/// </summary>
	public class StorageMessageAdapter : BufferMessageAdapter
	{
		private readonly StorageProcessor _storageProcessor;

		private static StorageProcessor CheckOnNull(StorageProcessor storageProcessor)
		{
			if (storageProcessor == null)
				throw new ArgumentNullException(nameof(storageProcessor));

			return storageProcessor;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="storageProcessor">Storage processor.</param>
		public StorageMessageAdapter(IMessageAdapter innerAdapter, StorageProcessor storageProcessor)
			: base(innerAdapter, CheckOnNull(storageProcessor).Buffer)
		{
			_storageProcessor = storageProcessor;
		}

		/// <inheritdoc />
		public override IEnumerable<object> GetCandleArgs(Type candleType, SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
		{
			var args = base.GetCandleArgs(candleType, securityId, from, to);

			var drive = _storageProcessor.Drive ?? _storageProcessor.StorageRegistry.DefaultDrive;

			if (drive == null)
				return args;

			return args.Concat(drive.GetCandleArgs(_storageProcessor.Format, candleType, securityId, from, to)).Distinct();
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					_storageProcessor.Reset();
					return base.OnSendInMessage(message);

				case MessageTypes.MarketData:
					return ProcessMarketData((MarketDataMessage)message);

				case MessageTypes.OrderStatus:
					return ProcessOrderStatus((OrderStatusMessage)message);

				case MessageTypes.OrderCancel:
					return ProcessOrderCancel((OrderCancelMessage)message);

				default:
					return base.OnSendInMessage(message);
			}
		}

		private bool ProcessOrderCancel(OrderCancelMessage message)
		{
			message = _storageProcessor.ProcessOrderCancel(message);

			return message == null || base.OnSendInMessage(message);
		}

		private bool ProcessOrderStatus(OrderStatusMessage message)
		{
			if (message.Adapter != null && message.Adapter != this)
				return base.OnSendInMessage(message);

			message = _storageProcessor.ProcessOrderStatus(message, RaiseStorageMessage);

			return message == null || base.OnSendInMessage(message);
		}

		private bool ProcessMarketData(MarketDataMessage message)
		{
			message = _storageProcessor.ProcessMarketData(message, RaiseStorageMessage);

			return message == null || base.OnSendInMessage(message);
		}

		private void RaiseStorageMessage(Message message)
		{
			message.TryInitLocalTime(this);

			RaiseNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new StorageMessageAdapter((IMessageAdapter)InnerAdapter.Clone(), _storageProcessor);
		}
	}
}