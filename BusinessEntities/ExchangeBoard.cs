#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: ExchangeBoard.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Configuration;
	using Ecng.Reflection;

	using MoreLinq;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Information about electronic board.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public partial class ExchangeBoard : Equatable<ExchangeBoard>, IExtendableEntity, IPersistable, INotifyPropertyChanged
	{
		private class InMemoryExchangeInfoProvider : IExchangeInfoProvider
		{
			private readonly CachedSynchronizedDictionary<string, ExchangeBoard> _boards = new CachedSynchronizedDictionary<string, ExchangeBoard>(StringComparer.InvariantCultureIgnoreCase);
			private readonly CachedSynchronizedDictionary<string, Exchange> _exchanges = new CachedSynchronizedDictionary<string, Exchange>(StringComparer.InvariantCultureIgnoreCase);

			IEnumerable<ExchangeBoard> IExchangeInfoProvider.Boards
			{
				get { return _boards.CachedValues; }
			}

			IEnumerable<Exchange> IExchangeInfoProvider.Exchanges
			{
				get { return _exchanges.CachedValues; }
			}

			ExchangeBoard IExchangeInfoProvider.GetExchangeBoard(string code)
			{
				return _boards.TryGetValue(code);
			}

			Exchange IExchangeInfoProvider.GetExchange(string code)
			{
				return _exchanges.TryGetValue(code);
			}

			void IExchangeInfoProvider.Save(ExchangeBoard board)
			{
				if (board == null)
					throw new ArgumentNullException(nameof(board));

				lock (_boards.SyncRoot)
				{
					if (!_boards.TryAdd(board.Code, board))
						return;
				}

				BoardAdded.SafeInvoke(board);
			}

			void IExchangeInfoProvider.Save(Exchange exchange)
			{
				if (exchange == null)
					throw new ArgumentNullException(nameof(exchange));

				lock (_exchanges.SyncRoot)
				{
					if (!_exchanges.TryAdd(exchange.Name, exchange))
						return;
				}

				ExchangeAdded.SafeInvoke(exchange);
			}

			public event Action<ExchangeBoard> BoardAdded;

			public event Action<Exchange> ExchangeAdded;

			public InMemoryExchangeInfoProvider()
			{
				EnumerateExchanges().ForEach(b => _exchanges[b.Name] = b);
				EnumerateExchangeBoards().ForEach(b => _boards[b.Code] = b);
			}
		}

		private static readonly SyncObject _syncObject = new SyncObject();
		private static IExchangeInfoProvider _exchangeInfoProvider;

		private static IExchangeInfoProvider ExchangeInfoProvider
		{
			get
			{
				if (_exchangeInfoProvider != null)
					return _exchangeInfoProvider;

				lock (_syncObject)
				{
					if (_exchangeInfoProvider == null)
					{
						_exchangeInfoProvider = ConfigManager.TryGetService<IExchangeInfoProvider>();

						if (_exchangeInfoProvider != null)
							return _exchangeInfoProvider;

						ConfigManager.RegisterService(_exchangeInfoProvider = new InMemoryExchangeInfoProvider());	
					}

					return _exchangeInfoProvider;
				}
			}
		}

		//private static IEnumerable<DateTime> GetDefaultRussianHolidays(DateTime startYear, DateTime endYear)
		//{
		//	if (startYear >= endYear)
		//		throw new ArgumentOutOfRangeException("endYear");

		//	var holidays = new List<DateTime>();

		//	for (var year = startYear.Year; year <= endYear.Year; year++)
		//	{
		//		for (var i = 1; i <= 10; i++)
		//			holidays.Add(new DateTime(year, 1, i));

		//		holidays.Add(new DateTime(year, 2, 23));
		//		holidays.Add(new DateTime(year, 3, 8));
		//		holidays.Add(new DateTime(year, 5, 1));
		//		holidays.Add(new DateTime(year, 5, 2));
		//		holidays.Add(new DateTime(year, 5, 9));
		//		holidays.Add(new DateTime(year, 6, 12));
		//		holidays.Add(new DateTime(year, 11, 4));
		//	}

		//	return holidays;
		//}

		private const BindingFlags _publicStatic = BindingFlags.Public | BindingFlags.Static;

		/// <summary>
		/// To get a list of exchanges.
		/// </summary>
		/// <returns>Exchanges.</returns>
		public static IEnumerable<Exchange> EnumerateExchanges()
		{
			return typeof(Exchange).GetMembers<PropertyInfo>(_publicStatic, typeof(Exchange))
				.Select(prop => (Exchange)prop.GetValue(null, null));
		}

		/// <summary>
		/// To get a list of boards.
		/// </summary>
		/// <returns>Boards.</returns>
		public static IEnumerable<ExchangeBoard> EnumerateExchangeBoards()
		{
			return typeof(ExchangeBoard).GetMembers<PropertyInfo>(_publicStatic, typeof(ExchangeBoard))
				.Select(prop => (ExchangeBoard)prop.GetValue(null, null));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExchangeBoard"/>.
		/// </summary>
		public ExchangeBoard()
		{
			ExtensionInfo = new Dictionary<object, object>();
		}

		private string _code = string.Empty;

		/// <summary>
		/// Board code.
		/// </summary>
		[DataMember]
		[Identity]
		[DisplayNameLoc(LocalizedStrings.CodeKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string Code
		{
			get { return _code; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (Code == value)
					return;

				_code = value;
				Notify("Code");
			}
		}

		private TimeSpan _expiryTime;

		/// <summary>
		/// Securities expiration times.
		/// </summary>
		[TimeSpan]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.Str64Key)]
		[MainCategory]
		public TimeSpan ExpiryTime
		{
			get { return _expiryTime; }
			set
			{
				if (ExpiryTime == value)
					return;

				_expiryTime = value;
				Notify("ExpiryTime");
			}
		}

		/// <summary>
		/// Exchange, where board is situated.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExchangeInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str479Key)]
		[MainCategory]
		public Exchange Exchange { get; set; }

		private bool _isSupportAtomicReRegister;

		/// <summary>
		/// Gets a value indicating whether the re-registration orders via <see cref="OrderReplaceMessage"/> as a single transaction.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ReregisteringKey)]
		[DescriptionLoc(LocalizedStrings.Str60Key)]
		[MainCategory]
		public bool IsSupportAtomicReRegister
		{
			get { return _isSupportAtomicReRegister; }
			set
			{
				_isSupportAtomicReRegister = value;
				Notify("IsSupportAtomicReRegister");
			}
		}

		private bool _isSupportMarketOrders;

		/// <summary>
		/// Are market type orders <see cref="OrderTypes.Market"/> supported.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarketOrdersKey)]
		[DescriptionLoc(LocalizedStrings.MarketOrdersSupportedKey)]
		[MainCategory]
		public bool IsSupportMarketOrders
		{
			get { return _isSupportMarketOrders; }
			set
			{
				_isSupportMarketOrders = value;
				Notify("IsSupportMarketOrders");
			}
		}

		private WorkingTime _workingTime = new WorkingTime();

		/// <summary>
		/// Board working hours.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.WorkingTimeKey)]
		[DescriptionLoc(LocalizedStrings.WorkingHoursKey)]
		[MainCategory]
		[InnerSchema]
		public WorkingTime WorkingTime
		{
			get { return _workingTime; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (WorkingTime == value)
					return;

				_workingTime = value;
				Notify("WorkingTime");
			}
		}

		[field: NonSerialized]
		private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

		/// <summary>
		/// Information about the time zone where the exchange is located.
		/// </summary>
		[TimeZoneInfo]
		[XmlIgnore]
		[DataMember]
		public TimeZoneInfo TimeZone
		{
			get { return _timeZone; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (TimeZone == value)
					return;

				_timeZone = value;
				Notify("TimeZone");
			}
		}

		///// <summary>
		///// Все площадки.
		///// </summary>
		//public static ExchangeBoard[] AllBoards
		//{
		//	get { return ExchangeInfoProvider.Boards; }
		//}

		/// <summary>
		/// To get a board by its code.
		/// </summary>
		/// <param name="code">Board code.</param>
		/// <returns>Found board. If board with the passed name does not exist, then <see langword="null" /> will be returned.</returns>
		public static ExchangeBoard GetBoard(string code)
		{
			return code.CompareIgnoreCase("RTS") ? Forts : ExchangeInfoProvider.GetExchangeBoard(code);
		}

		/// <summary>
		/// To get a board by its code. If board with the passed name does not exist, then it will be created.
		/// </summary>
		/// <param name="code">Board code.</param>
		/// <param name="createBoard">The handler creating a board, if it is not found. If the value is <see langword="null" />, then the board is created by default initialization.</param>
		/// <returns>Exchange board.</returns>
		public static ExchangeBoard GetOrCreateBoard(string code, Func<string, ExchangeBoard> createBoard = null)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			if (code.CompareIgnoreCase("RTS"))
				return Forts;

			var board = ExchangeInfoProvider.GetExchangeBoard(code);

			if (board != null)
				return board;

			if (createBoard == null)
			{
				var exchange = ExchangeInfoProvider.GetExchange(code);

				if (exchange == null)
				{
					exchange = new Exchange { Name = code };
					ExchangeInfoProvider.Save(exchange);
				}

				board = new ExchangeBoard
				{
					Code = code,
					Exchange = exchange
				};
			}
			else
			{
				board = createBoard(code);

				if (ExchangeInfoProvider.GetExchange(board.Exchange.Name) == null)
					ExchangeInfoProvider.Save(board.Exchange);
			}

			SaveBoard(board);

			return board;
		}

		/// <summary>
		/// To save the board.
		/// </summary>
		/// <param name="board">Board.</param>
		public static void SaveBoard(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			ExchangeInfoProvider.Save(board);
		}

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Extended exchange info.
		/// </summary>
		/// <remarks>
		/// Required if additional information associated with the exchange is stored in the program. .
		/// </remarks>
		[XmlIgnore]
		[Browsable(false)]
		[DataMember]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_extensionInfo = value;
				Notify("ExtensionInfo");
			}
		}

		[OnDeserialized]
		private void AfterDeserialization(StreamingContext ctx)
		{
			if (ExtensionInfo == null)
				ExtensionInfo = new Dictionary<object, object>();
		}

		[field: NonSerialized]
		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

		private void Notify(string info)
		{
			_propertyChanged.SafeInvoke(this, info);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "{0} ({1})".Put(Code, Exchange);
		}

		/// <summary>
		/// Compare <see cref="ExchangeBoard"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(ExchangeBoard other)
		{
			return Code == other.Code && Exchange == other.Exchange;
		}

		private int _hashCode;

		/// <summary>
		/// Get the hash code of the object <see cref="ExchangeBoard"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			if (_hashCode == 0)
				_hashCode = Code.GetHashCode() ^ (Exchange == null ? 0 : Exchange.GetHashCode());

			return _hashCode;
		}

		/// <summary>
		/// Create a copy of <see cref="ExchangeBoard"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override ExchangeBoard Clone()
		{
			return new ExchangeBoard
			{
				Exchange = Exchange,
				Code = Code,
				IsSupportAtomicReRegister = IsSupportAtomicReRegister,
				IsSupportMarketOrders = IsSupportMarketOrders,
				ExpiryTime = ExpiryTime,
				WorkingTime = WorkingTime.Clone(),
				TimeZone = TimeZone,
			};
		}

		/// <summary>
		/// Is MICEX board.
		/// </summary>
		public bool IsMicex
		{
			get { return Exchange == Exchange.Moex && this != Forts; }
		}

		/// <summary>
		/// Is the UX exchange stock market board.
		/// </summary>
		public bool IsUxStock
		{
			get { return Exchange == Exchange.Ux && this != Ux; }
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Exchange = storage.GetValue<SettingsStorage>("Exchange").Load<Exchange>();
			Code = storage.GetValue<string>("Code");
			IsSupportMarketOrders = storage.GetValue<bool>("IsSupportMarketOrders");
			IsSupportAtomicReRegister = storage.GetValue<bool>("IsSupportAtomicReRegister");
			ExpiryTime = storage.GetValue<TimeSpan>("ExpiryTime");
			WorkingTime = storage.GetValue<SettingsStorage>("WorkingTime").Load<WorkingTime>();
			TimeZone = storage.GetValue("TimeZone", TimeZone);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Exchange", Exchange.Save());
			storage.SetValue("Code", Code);
			storage.SetValue("IsSupportMarketOrders", IsSupportMarketOrders);
			storage.SetValue("IsSupportAtomicReRegister", IsSupportAtomicReRegister);
			storage.SetValue("ExpiryTime", ExpiryTime);
			storage.SetValue("WorkingTime", WorkingTime.Save());
			storage.SetValue("TimeZone", TimeZone);
		}
	}
}