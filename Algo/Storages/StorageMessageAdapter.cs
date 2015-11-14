namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using MoreLinq;

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
				throw new ArgumentNullException(nameof(entityRegistry));

			if (storageRegistry == null)
				throw new ArgumentNullException(nameof(storageRegistry));

			_entityRegistry = entityRegistry;
			_storageRegistry = storageRegistry;

			Drive = _storageRegistry.DefaultDrive;

			ThreadingHelper.Timer(() =>
			{
				foreach (var pair in GetTicks())
				{
					GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.Tick).Save(pair.Value);
				}

				foreach (var pair in GetOrderLog())
				{
					GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.OrderLog).Save(pair.Value);
				}

				foreach (var pair in GetTransactions())
				{
					GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.Order).Save(pair.Value);
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
				{
					_storageRegistry.GetNewsMessageStorage(Drive, Format).Save(news);
				}

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
					throw new ArgumentNullException(nameof(value));

				_drive = value;
			}
		}

		/// <summary>
		/// Format.
		/// </summary>
		public StorageFormats Format { get; set; }

		private TimeSpan _daysLoad;

		/// <summary>
		/// Total days to load stored data.
		/// </summary>
		public TimeSpan DaysLoad
		{
			get { return _daysLoad; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value));

				_daysLoad = value;
			}
		}

		private IMarketDataStorage<TMessage> GetStorage<TMessage>(SecurityId securityId, object arg)
			where TMessage : Message
        {
			return (IMarketDataStorage<TMessage>)GetStorage(securityId, typeof(TMessage), arg);
		}

		private IMarketDataStorage GetStorage(SecurityId securityId, Type messageType, object arg)
		{
			var security = _entityRegistry.Securities.ReadBySecurityId(securityId);
			return _storageRegistry.GetStorage(security, messageType, arg, Drive, Format);
		}

		/// <summary>
		/// Load save data from storage.
		/// </summary>
		public void Load()
		{
			var requiredSecurities = new List<SecurityId>();
			var availableSecurities = Drive.AvailableSecurities.ToHashSet();

			foreach (var security in _entityRegistry.Securities)
			{
				var msg = security.ToMessage();

				if (availableSecurities.Remove(msg.SecurityId))
				{
                    requiredSecurities.Add(msg.SecurityId);
				}

                RaiseNewOutMessage(msg);
			}

			foreach (var board in _entityRegistry.ExchangeBoards)
				RaiseNewOutMessage(board.ToMessage());

			if (DaysLoad == TimeSpan.Zero)
				return;

			foreach (var secId in requiredSecurities)
			{
				GetStorage<ExecutionMessage>(secId, ExecutionTypes.Tick)
					.Load(DateTimeOffset.Now - DaysLoad, DateTimeOffset.Now)
					.ForEach(RaiseNewOutMessage);

				GetStorage<ExecutionMessage>(secId, ExecutionTypes.Order)
					.Load(DateTimeOffset.Now - DaysLoad, DateTimeOffset.Now)
					.ForEach(RaiseNewOutMessage);

				GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog)
					.Load(DateTimeOffset.Now - DaysLoad, DateTimeOffset.Now)
					.ForEach(RaiseNewOutMessage);
			}

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