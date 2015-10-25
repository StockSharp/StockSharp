namespace StockSharp.Algo.Storages
{
	using System;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Storage based message adapter.
	/// </summary>
	public class StorageMessageAdapter : BufferMessageAdapter
	{
		private readonly IStorageRegistry _storageRegistry;
		private readonly IEntityRegistry _entityRegistry;

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		public StorageMessageAdapter(IMessageAdapter innerAdapter, IEntityRegistry entityRegistry, IStorageRegistry storageRegistry)
			: base(innerAdapter)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException("entityRegistry");

			if (storageRegistry == null)
				throw new ArgumentNullException("storageRegistry");

			_entityRegistry = entityRegistry;
			_storageRegistry = storageRegistry;

			Drive = _storageRegistry.DefaultDrive;

			ThreadingHelper.Timer(() =>
			{
				foreach (var pair in GetTicks())
				{
					GetStorage(pair.Key, typeof(ExecutionMessage), ExecutionTypes.Tick).Save(pair.Value);
				}

				foreach (var pair in GetOrderLog())
				{
					GetStorage(pair.Key, typeof(ExecutionMessage), ExecutionTypes.OrderLog).Save(pair.Value);
				}

				foreach (var pair in GetTransactions())
				{
					GetStorage(pair.Key, typeof(ExecutionMessage), ExecutionTypes.Order).Save(pair.Value);
				}

				foreach (var pair in GetOrderBooks())
				{
					GetStorage(pair.Key, typeof(QuoteChangeMessage), null).Save(pair.Value);
				}

				foreach (var pair in GetLevel1())
				{
					GetStorage(pair.Key, typeof(Level1ChangeMessage), null).Save(pair.Value);
				}

				foreach (var pair in GetCandles())
				{
					GetStorage(pair.Key.Item1, pair.Key.Item2, pair.Key.Item3).Save(pair.Value);
				}

				var news = GetNews().ToArray();

				if (news.Length > 0)
					_storageRegistry.GetNewsMessageStorage(Drive, Format).Save(news);

			}).Interval(TimeSpan.FromSeconds(10));
		}

		private IMarketDataDrive _drive;

		/// <summary>
		/// The storage (database, file etc.).
		/// </summary>
		public IMarketDataDrive Drive
		{
			get { return _drive; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_drive = value;
			}
		}

		/// <summary>
		/// Format.
		/// </summary>
		public StorageFormats Format { get; set; }

		/// <summary>
		/// Total days to load stored data.
		/// </summary>
		public TimeSpan DaysLoad { get; set; }

		private IMarketDataStorage GetStorage(SecurityId securityId, Type messageType, object arg)
		{
			var security = _entityRegistry.Securities.ReadById(securityId.SecurityCode + "@" + securityId.BoardCode);

			return _storageRegistry.GetStorage(security, messageType, arg, Drive, Format);
		}

		/// <summary>
		/// Load save data from storage.
		/// </summary>
		public void Load()
		{
			foreach (var security in _entityRegistry.Securities)
				RaiseNewOutMessage(security.ToMessage());

			foreach (var board in _entityRegistry.ExchangeBoards)
				RaiseNewOutMessage(board.ToMessage());

			//_storageRegistry.DefaultDrive.GetCandleTypes();
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Security:
					_entityRegistry.Securities.Add(((SecurityMessage)message).ToSecurity());
					break;
				case MessageTypes.Board:
					_entityRegistry.ExchangeBoards.Add(((BoardMessage)message).ToBoard());
					break;
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new StorageMessageAdapter(InnerAdapter, _entityRegistry, _storageRegistry);
		}
	}
}