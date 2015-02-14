namespace StockSharp.Algo.Testing
{
	using System;
	using System.ComponentModel;

	using Ecng.ComponentModel;
	using Ecng.Serialization;


	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Настройки эмулятора биржи.
	/// </summary>
	public class MarketEmulatorSettings : NotifiableObject, IPersistable
	{
		/// <summary>
		/// Создать <see cref="MarketEmulatorSettings"/>.
		/// </summary>
		public MarketEmulatorSettings()
		{
			DepthExpirationTime = TimeSpan.FromDays(1);
			MatchOnTouch = true;
			IsSupportAtomicReRegister = true;
		}

		private bool _matchOnTouch;

		/// <summary>
		/// При эмулировании сведения по сделкам, производить сведение заявок, когда цена сделки коснулась цены заявки (равна цене заявки),
		/// а не только, когда цена сделки лучше цены заявки. По-умолчанию включено (оптимистический сценарий).
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(50)]
		[DisplayNameLoc(LocalizedStrings.Str1176Key)]
		[DescriptionLoc(LocalizedStrings.Str1177Key)]
		public bool MatchOnTouch
		{
			get { return _matchOnTouch; }
			set
			{
				if (_matchOnTouch == value)
					return;

				_matchOnTouch = value;
				NotifyChanged("MatchOnTouch");
			}
		}

		private TimeSpan _depthExpirationTime;

		/// <summary>
		/// Максимальное время, которое стакан находится в эмуляторе. Если за это время не произошло обновление, стакан стирается.
		/// Это свойство можно использовать, чтобы убирать старые стаканы при наличии дыр в данных. По-умолчанию равно 1 дню.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(200)]
		[DisplayNameLoc(LocalizedStrings.Str1178Key)]
		[DescriptionLoc(LocalizedStrings.Str1179Key)]
		public TimeSpan DepthExpirationTime
		{
			get { return _depthExpirationTime; }
			set
			{
				if (_depthExpirationTime == value)
					return;

				_depthExpirationTime = value;
				NotifyChanged("DepthExpirationTime");
			}
		}

		private double _failing;

		/// <summary>
		/// Процентное значение ошибки регистрации новых заявок. Значение может быть от 0 (не будет ни одной ошибки) до 100.
		/// По-умолчанию выключено.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(60)]
		[DisplayNameLoc(LocalizedStrings.Str1180Key)]
		[DescriptionLoc(LocalizedStrings.Str1181Key)]
		public double Failing
		{
			get { return _failing; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1182);

				if (value > 100)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1183);

				_failing = value;
				NotifyChanged("Failing");
			}
		}

		private TimeSpan _latency;

		/// <summary>
		/// Минимальное значение задержки выставляемых заявок.
		/// По-умолчанию равно <see cref="TimeSpan.Zero"/>, что означает мгновенное принятие биржей выставляемых заявок.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(20)]
		[DisplayNameLoc(LocalizedStrings.Str161Key)]
		[DescriptionLoc(LocalizedStrings.Str1184Key)]
		public TimeSpan Latency
		{
			get { return _latency; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1185);

				_latency = value;
				NotifyChanged("Latency");
			}
		}

		private bool _isSupportAtomicReRegister;

		/// <summary>
		/// Поддерживается ли перерегистрация заявок в виде одной транзакции. По-умолчанию включено.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(30)]
		[DisplayName("MOVE")]
		[DescriptionLoc(LocalizedStrings.Str60Key)]
		public bool IsSupportAtomicReRegister
		{
			get { return _isSupportAtomicReRegister; }
			set
			{
				_isSupportAtomicReRegister = value;
				NotifyChanged("_isSupportAtomicReRegister");
			}
		}

		private TimeSpan _bufferTime;

		/// <summary>
		/// Отправлять ответы интервально целым пакетом. Эмулируется сетевая задержка и буферизированная работа биржевого ядра.
		/// По умолчанию 0 мс.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(80)]
		[DisplayNameLoc(LocalizedStrings.Str1186Key)]
		[DescriptionLoc(LocalizedStrings.Str1187Key)]
		public TimeSpan BufferTime
		{
			get { return _bufferTime; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str940);

				_bufferTime = value;
				NotifyChanged("BufferTime");
			}
		}

		private TimeSpan? _useCandlesTimeFrame;

		/// <summary>
		/// Использовать свечи с заданным тайм-фреймом. Если тайм-фрейм равен <see langword="null"/>, свечи не используются.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(10)]
		[DisplayNameLoc(LocalizedStrings.CandlesKey)]
		[DescriptionLoc(LocalizedStrings.Str1188Key)]
		[Nullable]
		[DefaultValue(typeof(TimeSpan), "00:05:00")]
		public TimeSpan? UseCandlesTimeFrame
		{
			get { return _useCandlesTimeFrame; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1189);

				_useCandlesTimeFrame = value;
				NotifyChanged("UseCandlesTimeFrame");
			}
		}

		private long _initialOrderId;

		/// <summary>
		/// Номер, начиная с которого эмулятор будет генерировать номера для заявок <see cref="Order.Id"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(70)]
		[DisplayNameLoc(LocalizedStrings.Str1190Key)]
		[DescriptionLoc(LocalizedStrings.Str1191Key)]
		public long InitialOrderId
		{
			get { return _initialOrderId; }
			set
			{
				_initialOrderId = value;
				NotifyChanged("InitialOrderId");
			}
		}

		private long _initialTradeId;

		/// <summary>
		/// Номер, начиная с которого эмулятор будет генерировать номера для сделок <see cref="Trade.Id"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(80)]
		[DisplayNameLoc(LocalizedStrings.Str1192Key)]
		[DescriptionLoc(LocalizedStrings.Str1193Key)]
		public long InitialTradeId
		{
			get { return _initialTradeId; }
			set
			{
				_initialTradeId = value;
				NotifyChanged("InitialTradeId");
			}
		}

		private long _initialTransactionId;

		/// <summary>
		/// Номер, начиная с которого эмулятор будет генерировать номера для транзакций заявок <see cref="Order.TransactionId"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(80)]
		[DisplayNameLoc(LocalizedStrings.Str230Key)]
		[DescriptionLoc(LocalizedStrings.Str1194Key)]
		public long InitialTransactionId
		{
			get { return _initialTransactionId; }
			set
			{
				_initialTransactionId = value;
				NotifyChanged("InitialTransactionId");
			}
		}

		private int _spreadSize = 2;

		/// <summary>
		/// Размер спреда в шагах цены. Используется при определение спреда для генерации стакана из тиковых сделок.
		/// По-умолчанию равен 2.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(90)]
		[DisplayNameLoc(LocalizedStrings.Str1195Key)]
		[DescriptionLoc(LocalizedStrings.Str1196Key)]
		public int SpreadSize
		{
			get { return _spreadSize; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_spreadSize = value;
				NotifyChanged("SpreadSize");
			}
		}

		private int _maxDepth = 5;

		/// <summary>
		/// Максимальная глубина стакана, который будет генерироваться из тиков.
		/// Используется, если нет истории стаканов. По-умолчанию равно 5.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(100)]
		[DisplayNameLoc(LocalizedStrings.Str1197Key)]
		[DescriptionLoc(LocalizedStrings.Str1198Key)]
		public int MaxDepth
		{
			get { return _maxDepth; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_maxDepth = value;
				NotifyChanged("MaxDepth");
			}
		}

		private int _volumeMultiplier = 2;

		/// <summary>
		/// Количество шагов объема, на которое заявка больше тиковой сделки. Используется при тестировании на тиковых сделках.
		/// По-умолчанию равен 2.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(110)]
		[DisplayNameLoc(LocalizedStrings.Str1199Key)]
		[DescriptionLoc(LocalizedStrings.Str1200Key)]
		public int VolumeMultiplier
		{
			get { return _volumeMultiplier; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_volumeMultiplier = value;
				NotifyChanged("VolumeMultiplier");
			}
		}

		private TimeSpan _portfolioRecalcInterval = TimeSpan.Zero;

		/// <summary>
		/// Интервал перерасчета данных по портфелям. Если интервал равен <see cref="TimeSpan.Zero"/>, то перерасчет не выполняется.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(120)]
		[DisplayNameLoc(LocalizedStrings.Str1201Key)]
		[DescriptionLoc(LocalizedStrings.Str1202Key)]
		public TimeSpan PortfolioRecalcInterval
		{
			get { return _portfolioRecalcInterval; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str940);

				_portfolioRecalcInterval = value;
				NotifyChanged("PortfolioRecalcInterval");
			}
		}

		private bool _convertTime;

		/// <summary>
		/// Переводить время для заявок и сделок в биржевое. По-умолчанию выключено.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(130)]
		[DisplayNameLoc(LocalizedStrings.Str1203Key)]
		[DescriptionLoc(LocalizedStrings.Str1204Key)]
		public bool ConvertTime
		{
			get { return _convertTime; }
			set
			{
				_convertTime = value;
				NotifyChanged("ConvertTime");
			}
		}

		private Unit _priceLimitOffset = new Unit(40, UnitTypes.Percent);

		/// <summary>
		/// Сдвиг цены от последней сделки, определяющие границы максимальной и минимальной цен на следующую сессию.
		/// Используется только, если нет сохраненной информации <see cref="Level1ChangeMessage"/>. По-умолчанию равен 40%.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(140)]
		[DisplayNameLoc(LocalizedStrings.Str1205Key)]
		[DescriptionLoc(LocalizedStrings.Str1206Key)]
		public Unit PriceLimitOffset
		{
			get { return _priceLimitOffset; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_priceLimitOffset = value;
			}
		}

		private bool _increaseDepthVolume = true;

		/// <summary>
		/// Добавлять дополнительный объем в стакан при выставлении заявок с большим объемом. По-умолчанию включено.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1175Key)]
		[PropertyOrder(150)]
		[DisplayNameLoc(LocalizedStrings.Str1207Key)]
		[DescriptionLoc(LocalizedStrings.Str1208Key)]
		public bool IncreaseDepthVolume
		{
			get { return _increaseDepthVolume; }
			set
			{
				_increaseDepthVolume = value;
				NotifyChanged("IncreaseDepthVolume");
			}
		}

		/// <summary>
		/// Сохранить состояние параметров эмуляции.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("DepthExpirationTime", DepthExpirationTime);
			storage.SetValue("MatchOnTouch", MatchOnTouch);
			storage.SetValue("Failing", Failing);
			storage.SetValue("Latency", Latency);
			storage.SetValue("IsSupportAtomicReRegister", IsSupportAtomicReRegister);
			storage.SetValue("BufferTime", BufferTime);
			storage.SetValue("UseCandlesTimeFrame", UseCandlesTimeFrame);
			storage.SetValue("InitialOrderId", InitialOrderId);
			storage.SetValue("InitialTradeId", InitialTradeId);
			storage.SetValue("InitialTransactionId", InitialTransactionId);
			storage.SetValue("SpreadSize", SpreadSize);
			storage.SetValue("MaxDepth", MaxDepth);
			storage.SetValue("VolumeMultiplier", VolumeMultiplier);
			storage.SetValue("PortfolioRecalcInterval", PortfolioRecalcInterval);
			storage.SetValue("ConvertTime", ConvertTime);
			storage.SetValue("PriceLimitOffset", PriceLimitOffset);
			storage.SetValue("IncreaseDepthVolume", IncreaseDepthVolume);
		}

		/// <summary>
		/// Загрузить состояние параметров эмуляции.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public virtual void Load(SettingsStorage storage)
		{
			DepthExpirationTime = storage.GetValue("DepthExpirationTime", DepthExpirationTime);
			MatchOnTouch = storage.GetValue("MatchOnTouch", MatchOnTouch);
			Failing = storage.GetValue("Failing", Failing);
			Latency = storage.GetValue("Latency", Latency);
			IsSupportAtomicReRegister = storage.GetValue("IsSupportAtomicReRegister", IsSupportAtomicReRegister);
			BufferTime = storage.GetValue("BufferTime", BufferTime);
			UseCandlesTimeFrame = storage.GetValue("UseCandlesTimeFrame", UseCandlesTimeFrame);
			InitialOrderId = storage.GetValue("InitialOrderId", InitialOrderId);
			InitialTradeId = storage.GetValue("InitialTradeId", InitialTradeId);
			InitialTransactionId = storage.GetValue("InitialTransactionId", InitialTransactionId);
			SpreadSize = storage.GetValue("SpreadSize", SpreadSize);
			MaxDepth = storage.GetValue("MaxDepth", MaxDepth);
			VolumeMultiplier = storage.GetValue("VolumeMultiplier", VolumeMultiplier);
			PortfolioRecalcInterval = storage.GetValue("PortfolioRecalcInterval", PortfolioRecalcInterval);
			ConvertTime = storage.GetValue("ConvertTime", ConvertTime);
			PriceLimitOffset = storage.GetValue("PriceLimitOffset", PriceLimitOffset);
			IncreaseDepthVolume = storage.GetValue("IncreaseDepthVolume", IncreaseDepthVolume);
		}
	}
}