namespace StockSharp.Algo.Risk
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.CompilerServices;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Base risk-rule.
	/// </summary>
	public abstract class RiskRule : BaseLogReceiver, IRiskRule, INotifyPropertyChanged
	{
		/// <summary>
		/// Initialize <see cref="RiskRule"/>.
		/// </summary>
		protected RiskRule()
		{
			UpdateTitle();
		}

		/// <inheritdoc/>
		[Browsable(false)]
		public override Guid Id { get => base.Id; set => base.Id = value; }

		/// <inheritdoc/>
		[Browsable(false)]
		public override string Name { get => base.Name; set => base.Name = value; }

		/// <summary>
		/// Get title.
		/// </summary>
		protected abstract string GetTitle();

		/// <summary>
		/// Update title.
		/// </summary>
		protected void UpdateTitle() => Title = GetTitle();

		private string _title;

		/// <summary>
		/// Header.
		/// </summary>
		[Browsable(false)]
		public string Title
		{
			get => _title;
			private set
			{
				_title = value;
				NotifyChanged();
			}
		}

		private RiskActions _action;

		/// <inheritdoc />
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ActionKey,
			Description = LocalizedStrings.RiskRuleActionKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public RiskActions Action
		{
			get => _action;
			set
			{
				if (_action == value)
					return;

				_action = value;
				NotifyChanged();
			}
		}

		/// <inheritdoc />
		public virtual void Reset()
		{
		}

		/// <inheritdoc />
		public abstract bool ProcessMessage(Message message);

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			Action = storage.GetValue<RiskActions>(nameof(Action));

			base.Load(storage);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Action), Action.To<string>());

			base.Save(storage);
		}

		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add => _propertyChanged += value;
			remove => _propertyChanged -= value;
		}

		private void NotifyChanged([CallerMemberName]string propertyName = null)
		{
			_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	/// <summary>
	/// Risk-rule, tracking profit-loss.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PnLKey,
		Description = LocalizedStrings.RulePnLKey,
		GroupName = LocalizedStrings.PnLKey)]
	public class RiskPnLRule : RiskRule
	{
		private decimal? _initValue;

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();
			_initValue = null;
		}

		private Unit _pnL = new();

		/// <summary>
		/// Profit-loss.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PnLKey,
			Description = LocalizedStrings.PnLKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public Unit PnL
		{
			get => _pnL;
			set
			{
				if (_pnL == value)
					return;

				_pnL = value ?? throw new ArgumentNullException(nameof(value));
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _pnL?.To<string>();

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.PositionChange)
				return false;

			var pfMsg = (PositionChangeMessage)message;

			if (!pfMsg.IsMoney())
				return false;

			var currValue = pfMsg.TryGetDecimal(PositionChangeTypes.CurrentValue);

			if (currValue == null)
				return false;

			if (_initValue == null)
			{
				_initValue = currValue.Value;
				return false;
			}

			if (PnL.Type == UnitTypes.Limit)
			{
				if (PnL.Value > 0)
					return PnL.Value <= currValue.Value;
				else
					return PnL.Value >= currValue.Value;
			}

			if (PnL.Value > 0)
				return (_initValue + PnL) <= currValue.Value;
			else
				return (_initValue + PnL) >= currValue.Value;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(PnL), PnL);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			PnL = storage.GetValue<Unit>(nameof(PnL));
		}
	}

	/// <summary>
	/// Risk-rule, tracking position size.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PositionKey,
		Description = LocalizedStrings.RulePositionKey,
		GroupName = LocalizedStrings.PositionsKey)]
	public class RiskPositionSizeRule : RiskRule
	{
		private decimal _position;

		/// <summary>
		/// Position size.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PositionKey,
			Description = LocalizedStrings.PositionSizeKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public decimal Position
		{
			get => _position;
			set
			{
				if (_position == value)
					return;

				_position = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _position.To<string>();

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.PositionChange)
				return false;

			var posMsg = (PositionChangeMessage)message;
			var currValue = posMsg.TryGetDecimal(PositionChangeTypes.CurrentValue);

			if (currValue == null)
				return false;

			if (Position > 0)
				return currValue >= Position;
			else
				return currValue <= Position;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Position), Position);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Position = storage.GetValue<decimal>(nameof(Position));
		}
	}

	/// <summary>
	/// Risk-rule, tracking position lifetime.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PositionTimeKey,
		Description = LocalizedStrings.RulePositionTimeKey,
		GroupName = LocalizedStrings.PositionsKey)]
	public class RiskPositionTimeRule : RiskRule
	{
		private readonly Dictionary<Tuple<SecurityId, string>, DateTimeOffset> _posOpenTime = new();
		private TimeSpan _time;

		/// <summary>
		/// Position lifetime.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TimeKey,
			Description = LocalizedStrings.PositionTimeDescKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public TimeSpan Time
		{
			get => _time;
			set
			{
				if (_time == value)
					return;

				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_time = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _time.To<string>();

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();
			_posOpenTime.Clear();
		}

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.PositionChange:
				{
					var posMsg = (PositionChangeMessage)message;
					var currValue = posMsg.TryGetDecimal(PositionChangeTypes.CurrentValue);

					if (currValue == null)
						return false;

					var key = Tuple.Create(posMsg.SecurityId, posMsg.PortfolioName);

					if (currValue == 0)
					{
						_posOpenTime.Remove(key);
						return false;
					}

					if (!_posOpenTime.TryGetValue(key, out var openTime))
					{
						_posOpenTime.Add(key, posMsg.LocalTime);
						return false;
					}

					var diff = posMsg.LocalTime - openTime;

					if (diff < Time)
						return false;

					_posOpenTime.Remove(key);
					return true;
				}

				case MessageTypes.Time:
				{
					List<Tuple<SecurityId, string>> removingPos = null;

					foreach (var pair in _posOpenTime)
					{
						var diff = message.LocalTime - pair.Value;

						if (diff < Time)
							continue;

						if (removingPos == null)
							removingPos = new List<Tuple<SecurityId, string>>();

						removingPos.Add(pair.Key);
					}

					removingPos?.ForEach(t => _posOpenTime.Remove(t));

					return removingPos != null;
				}
			}

			return false;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Time), Time);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Time = storage.GetValue<TimeSpan>(nameof(Time));
		}
	}

	/// <summary>
	/// Risk-rule, tracking commission size.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.RiskCommissionKey,
		GroupName = LocalizedStrings.PnLKey)]
	public class RiskCommissionRule : RiskRule
	{
		private decimal _commission;

		/// <summary>
		/// Commission size.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CommissionKey,
			Description = LocalizedStrings.CommissionDescKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public decimal Commission
		{
			get => _commission;
			set
			{
				if (_commission == value)
					return;

				_commission = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _commission.To<string>();

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.PositionChange)
				return false;

			var pfMsg = (PositionChangeMessage)message;

			if (!pfMsg.IsMoney())
				return false;

			var currValue = pfMsg.TryGetDecimal(PositionChangeTypes.Commission);

			if (currValue == null)
				return false;

			return currValue >= Commission;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Commission), Commission);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Commission = storage.GetValue<decimal>(nameof(Commission));
		}
	}

	/// <summary>
	/// Risk-rule, tracking slippage size.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SlippageKey,
		Description = LocalizedStrings.RiskSlippageKey,
		GroupName = LocalizedStrings.OrdersKey)]
	public class RiskSlippageRule : RiskRule
	{
		private decimal _slippage;

		/// <summary>
		/// Slippage size.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SlippageKey,
			Description = LocalizedStrings.SlippageSizeKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public decimal Slippage
		{
			get => _slippage;
			set
			{
				if (_slippage == value)
					return;

				_slippage = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _slippage.To<string>();

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;
			var currValue = execMsg.Slippage;

			if (currValue == null)
				return false;

			if (Slippage > 0)
				return currValue > Slippage;
			else
				return currValue < Slippage;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Slippage), Slippage);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Slippage = storage.GetValue<decimal>(nameof(Slippage));
		}
	}

	/// <summary>
	/// Risk-rule, tracking order price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderPrice2Key,
		Description = LocalizedStrings.RiskOrderPriceKey,
		GroupName = LocalizedStrings.OrdersKey)]
	public class RiskOrderPriceRule : RiskRule
	{
		private decimal _price;

		/// <summary>
		/// Order price.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PriceKey,
			Description = LocalizedStrings.OrderPriceKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public decimal Price
		{
			get => _price;
			set
			{
				if (_price == value)
					return;

				_price = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _price.To<string>();

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var orderReg = (OrderRegisterMessage)message;
					return orderReg.Price >= Price;
				}

				case MessageTypes.OrderReplace:
				{
					var orderReplace = (OrderReplaceMessage)message;
					return orderReplace.Price > 0 && orderReplace.Price >= Price;
				}

				default:
					return false;
			}
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Price), Price);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Price = storage.GetValue<decimal>(nameof(Price));
		}
	}

	/// <summary>
	/// Risk-rule, tracking order volume.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderVolume2Key,
		Description = LocalizedStrings.RiskOrderVolumeKey,
		GroupName = LocalizedStrings.OrdersKey)]
	public class RiskOrderVolumeRule : RiskRule
	{
		private decimal _volume;

		/// <summary>
		/// Order volume.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VolumeKey,
			Description = LocalizedStrings.OrderVolumeKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public decimal Volume
		{
			get => _volume;
			set
			{
				if (_volume == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_volume = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _volume.To<string>();

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var orderReg = (OrderRegisterMessage)message;
					return orderReg.Volume >= Volume;
				}

				case MessageTypes.OrderReplace:
				{
					var orderReplace = (OrderReplaceMessage)message;
					return orderReplace.Volume > 0 && orderReplace.Volume >= Volume;
				}

				default:
					return false;
			}
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Volume), Volume);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Volume = storage.GetValue<decimal>(nameof(Volume));
		}
	}

	/// <summary>
	/// Risk-rule, tracking orders placing frequency.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderFreqKey,
		Description = LocalizedStrings.RiskOrderFreqKey,
		GroupName = LocalizedStrings.OrdersKey)]
	public class RiskOrderFreqRule : RiskRule
	{
		private DateTimeOffset? _endTime;
		private int _current;

		/// <inheritdoc />
		protected override string GetTitle() => Count + " -> " + Interval;

		private int _count;

		/// <summary>
		/// Order count.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CountKey,
			Description = LocalizedStrings.OrdersCountKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public int Count
		{
			get => _count;
			set
			{
				if (_count == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_count = value;
				UpdateTitle();
			}
		}

		private TimeSpan _interval;


		/// <summary>
		/// Interval, during which orders quantity will be monitored.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IntervalKey,
			Description = LocalizedStrings.RiskIntervalDescKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 1)]
		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (_interval == value)
					return;

				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_interval = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			_current = 0;
			_endTime = null;
		}

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderPairReplace:
				{
					var time = message.LocalTime;

					if (time == default)
					{
						this.AddWarningLog("Time is null. Msg={0}", message);
						return false;
					}

					if (_endTime == null)
					{
						_endTime = time + Interval;
						_current = 1;

						this.AddDebugLog("EndTime={0}", _endTime);
						return false;
					}

					if (time < _endTime)
					{
						_current++;

						this.AddDebugLog("Count={0} Msg={1}", _current, message);

						if (_current >= Count)
						{
							this.AddInfoLog("Count={0} EndTime={1}", _current, _endTime);

							_endTime = null;
							return true;
						}
					}
					else
					{
						_endTime = time + Interval;
						_current = 1;

						this.AddDebugLog("EndTime={0}", _endTime);
					}

					return false;
				}
			}

			return false;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Count), Count);
			storage.SetValue(nameof(Interval), Interval);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>(nameof(Count));
			Interval = storage.GetValue<TimeSpan>(nameof(Interval));
		}
	}

	/// <summary>
	/// Risk-rule, tracking orders error count.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderErrorKey,
		Description = LocalizedStrings.RiskOrderErrorKey,
		GroupName = LocalizedStrings.OrdersKey)]
	public class RiskOrderErrorRule : RiskRule
	{
		private int _current;

		private int _count;

		/// <summary>
		/// Error count.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ErrorsKey,
			Description = LocalizedStrings.ErrorsCountKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public int Count
		{
			get => _count;
			set
			{
				if (_count == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_count = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => Count.To<string>();

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			_current = 0;
		}

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (execMsg.Error is null)
			{
				if (execMsg.HasOrderInfo() && execMsg.OrderState == OrderStates.Active)
					_current = 0;

				return false;
			}

			return ++_current >= Count;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Count), Count);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>(nameof(Count));
		}
	}

	/// <summary>
	/// Risk-rule, tracking trade price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradePriceKey,
		Description = LocalizedStrings.RiskTradePriceKey,
		GroupName = LocalizedStrings.TradesKey)]
	public class RiskTradePriceRule : RiskRule
	{
		private decimal _price;

		/// <summary>
		/// Trade price.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PriceKey,
			Description = LocalizedStrings.TradePriceDescKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public decimal Price
		{
			get => _price;
			set
			{
				if (_price == value)
					return;

				_price = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _price.To<string>();

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (!execMsg.HasTradeInfo())
				return false;

			return execMsg.TradePrice >= Price;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Price), Price);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Price = storage.GetValue<decimal>(nameof(Price));
		}
	}

	/// <summary>
	/// Risk-rule, tracking trade volume.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradeVolumeKey,
		Description = LocalizedStrings.RiskTradeVolumeKey,
		GroupName = LocalizedStrings.TradesKey)]
	public class RiskTradeVolumeRule : RiskRule
	{
		private decimal _volume;

		/// <summary>
		/// Trade volume.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VolumeKey,
			Description = LocalizedStrings.TradeVolumeDescKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public decimal Volume
		{
			get => _volume;
			set
			{
				if (_volume == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_volume = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _volume.To<string>();

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (!execMsg.HasTradeInfo())
				return false;

			return execMsg.TradeVolume >= Volume;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Volume), Volume);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Volume = storage.GetValue<decimal>(nameof(Volume));
		}
	}

	/// <summary>
	/// Risk-rule, tracking orders execution frequency.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradeFreqKey,
		Description = LocalizedStrings.RiskTradeFreqKey,
		GroupName = LocalizedStrings.TradesKey)]
	public class RiskTradeFreqRule : RiskRule
	{
		private DateTimeOffset? _endTime;
		private int _current;

		/// <inheritdoc />
		protected override string GetTitle() => Count + " -> " + Interval;

		private int _count;

		/// <summary>
		/// Number of trades.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CountKey,
			Description = LocalizedStrings.LimitOrderTifKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public int Count
		{
			get => _count;
			set
			{
				if (_count == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_count = value;
				UpdateTitle();
			}
		}

		private TimeSpan _interval;

		/// <summary>
		/// Interval, during which trades quantity will be monitored.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IntervalKey,
			Description = LocalizedStrings.TradesIntervalKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 1)]
		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (_interval == value)
					return;

				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_interval = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			_current = 0;
			_endTime = null;
		}

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (!execMsg.HasTradeInfo())
				return false;

			var time = message.LocalTime;

			if (time == default)
			{
				this.AddWarningLog("Time is null. Msg={0}", message);
				return false;
			}

			if (_endTime == null)
			{
				_endTime = time + Interval;
				_current = 1;

				this.AddDebugLog("EndTime={0}", _endTime);
				return false;
			}

			if (time < _endTime)
			{
				_current++;

				this.AddDebugLog("Count={0} Msg={1}", _current, message);

				if (_current >= Count)
				{
					this.AddInfoLog("Count={0} EndTime={1}", _current, _endTime);

					_endTime = null;
					return true;
				}
			}
			else
			{
				_endTime = time + Interval;
				_current = 1;

				this.AddDebugLog("EndTime={0}", _endTime);
			}

			return false;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Count), Count);
			storage.SetValue(nameof(Interval), Interval);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>(nameof(Count));
			Interval = storage.GetValue<TimeSpan>(nameof(Interval));
		}
	}

	/// <summary>
	/// Risk-rule, tracking error count.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ErrorKey,
		Description = LocalizedStrings.RiskErrorKey,
		GroupName = LocalizedStrings.StrategyKey)]
	public class RiskErrorRule : RiskRule
	{
		private int _current;

		private int _count;

		/// <summary>
		/// Error count.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ErrorsKey,
			Description = LocalizedStrings.ErrorsCountKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public int Count
		{
			get => _count;
			set
			{
				if (_count == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_count = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => Count.To<string>();

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			_current = 0;
		}

		/// <inheritdoc />
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Error)
				return false;

			return ++_current >= Count;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Count), Count);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>(nameof(Count));
		}
	}
}