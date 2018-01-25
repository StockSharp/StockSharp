#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Risk.Algo
File: RiskRule.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Risk
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Collections;

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
		}

		private string _title;

		/// <summary>
		/// Header.
		/// </summary>
		[Browsable(false)]
		public string Title
		{
			get => _title;
			protected set
			{
				_title = value;
				NotifyChanged(nameof(Title));
			}
		}

		private RiskActions _action;

		/// <summary>
		/// Action that needs to be taken in case of rule activation.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str722Key)]
		[DescriptionLoc(LocalizedStrings.Str859Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RiskActions Action
		{
			get => _action;
			set
			{
				_action = value;
				NotifyChanged(nameof(Action));
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public virtual void Reset()
		{
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		public abstract bool ProcessMessage(Message message);

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			Action = storage.GetValue<RiskActions>(nameof(Action));

			base.Load(storage);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
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

		private void NotifyChanged(string propertyName)
		{
			_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	/// <summary>
	/// Risk-rule, tracking profit-loss.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.PnLKey)]
	[DescriptionLoc(LocalizedStrings.Str860Key)]
	public class RiskPnLRule : RiskRule
	{
		private decimal _pnL;

		/// <summary>
		/// Profit-loss.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.PnLKey)]
		[DescriptionLoc(LocalizedStrings.Str861Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal PnL
		{
			get => _pnL;
			set
			{
				_pnL = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.PortfolioChange)
				return false;

			var pfMsg = (PortfolioChangeMessage)message;
			var currValue = (decimal?)pfMsg.Changes.TryGetValue(PositionChangeTypes.CurrentValue);

			if (currValue == null)
				return false;

			if (PnL > 0)
				return currValue >= PnL;
			else
				return currValue <= PnL;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(PnL), PnL);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			PnL = storage.GetValue<decimal>(nameof(PnL));
		}
	}

	/// <summary>
	/// Risk-rule, tracking position size.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str862Key)]
	[DescriptionLoc(LocalizedStrings.Str863Key)]
	public class RiskPositionSizeRule : RiskRule
	{
		private decimal _position;

		/// <summary>
		/// Position size.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str862Key)]
		[DescriptionLoc(LocalizedStrings.Str864Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Position
		{
			get => _position;
			set
			{
				_position = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.PositionChange)
				return false;

			var posMsg = (PositionChangeMessage)message;
			var currValue = (decimal?)posMsg.Changes.TryGetValue(PositionChangeTypes.CurrentValue);

			if (currValue == null)
				return false;

			if (Position > 0)
				return currValue >= Position;
			else
				return currValue <= Position;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Position), Position);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Position = storage.GetValue<decimal>(nameof(Position));
		}
	}

	/// <summary>
	/// Risk-rule, tracking position lifetime.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str865Key)]
	[DescriptionLoc(LocalizedStrings.Str866Key)]
	public class RiskPositionTimeRule : RiskRule
	{
		private readonly Dictionary<Tuple<SecurityId, string>, DateTimeOffset> _posOpenTime = new Dictionary<Tuple<SecurityId, string>, DateTimeOffset>();
		private TimeSpan _time;

		/// <summary>
		/// Position lifetime.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TimeKey)]
		[DescriptionLoc(LocalizedStrings.Str867Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public TimeSpan Time
		{
			get => _time;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);
				
				_time = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_posOpenTime.Clear();
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		public override bool ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.PositionChange:
				{
					var posMsg = (PositionChangeMessage)message;
					var currValue = (decimal?)posMsg.Changes.TryGetValue(PositionChangeTypes.CurrentValue);

					if (currValue == null)
						return false;

					var key = Tuple.Create(posMsg.SecurityId, posMsg.PortfolioName);

					if (currValue == 0)
					{
						_posOpenTime.Remove(key);
						return false;
					}

					var openTime = _posOpenTime.TryGetValue2(key);

					if (openTime == null)
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

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Time), Time);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Time = storage.GetValue<TimeSpan>(nameof(Time));
		}
	}

	/// <summary>
	/// Risk-rule, tracking commission size.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str159Key)]
	[DescriptionLoc(LocalizedStrings.Str868Key)]
	public class RiskCommissionRule : RiskRule
	{
		private decimal _commission;

		/// <summary>
		/// Commission size.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str869Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Commission
		{
			get => _commission;
			set
			{
				_commission = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.PortfolioChange)
				return false;

			var pfMsg = (PortfolioChangeMessage)message;
			var currValue = (decimal?)pfMsg.Changes.TryGetValue(PositionChangeTypes.Commission);

			if (currValue == null)
				return false;

			return currValue >= Commission;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Commission), Commission);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Commission = storage.GetValue<decimal>(nameof(Commission));
		}
	}

	/// <summary>
	/// Risk-rule, tracking slippage size.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str163Key)]
	[DescriptionLoc(LocalizedStrings.Str870Key)]
	public class RiskSlippageRule : RiskRule
	{
		private decimal _slippage;

		/// <summary>
		/// Slippage size.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str871Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Slippage
		{
			get => _slippage;
			set
			{
				_slippage = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
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

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Slippage), Slippage);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Slippage = storage.GetValue<decimal>(nameof(Slippage));
		}
	}

	/// <summary>
	/// Risk-rule, tracking order price.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str872Key)]
	[DescriptionLoc(LocalizedStrings.Str873Key)]
	public class RiskOrderPriceRule : RiskRule
	{
		private decimal _price;

		/// <summary>
		/// Order price.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.OrderPriceKey)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Price
		{
			get => _price;
			set
			{
				_price = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
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

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Price), Price);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Price = storage.GetValue<decimal>(nameof(Price));
		}
	}

	/// <summary>
	/// Risk-rule, tracking order volume.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str662Key)]
	[DescriptionLoc(LocalizedStrings.Str874Key)]
	public class RiskOrderVolumeRule : RiskRule
	{
		private decimal _volume;

		/// <summary>
		/// Order volume.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str875Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Volume
		{
			get => _volume;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_volume = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
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

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Volume), Volume);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Volume = storage.GetValue<decimal>(nameof(Volume));
		}
	}

	/// <summary>
	/// Risk-rule, tracking orders placing frequency.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str876Key)]
	[DescriptionLoc(LocalizedStrings.Str877Key)]
	public class RiskOrderFreqRule : RiskRule
	{
		private DateTimeOffset? _endTime;
		private int _current;

		private void UpdateTitle()
		{
			Title = Count + " -> " + Interval;
		}

		private int _count;

		/// <summary>
		/// Order count.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str878Key)]
		[DescriptionLoc(LocalizedStrings.Str957Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get => _count;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_count = value;
				UpdateTitle();
			}
		}

		private TimeSpan _interval;


		/// <summary>
		/// Interval, during which orders quantity will be monitored.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str175Key)]
		[DescriptionLoc(LocalizedStrings.Str879Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_interval = value;
				UpdateTitle();
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_current = 0;
			_endTime = null;
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		public override bool ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderPairReplace:
				{
					var time = message.LocalTime;

					if (time.IsDefault())
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

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Count), Count);
			storage.SetValue(nameof(Interval), Interval);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>(nameof(Count));
			Interval = storage.GetValue<TimeSpan>(nameof(Interval));
		}
	}

	/// <summary>
	/// Risk-rule, tracking trade price.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str672Key)]
	[DescriptionLoc(LocalizedStrings.Str880Key)]
	public class RiskTradePriceRule : RiskRule
	{
		private decimal _price;

		/// <summary>
		/// Trade price.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str147Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Price
		{
			get => _price;
			set
			{
				_price = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (!execMsg.HasTradeInfo())
				return false;

			return execMsg.TradePrice >= Price;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Price), Price);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Price = storage.GetValue<decimal>(nameof(Price));
		}
	}

	/// <summary>
	/// Risk-rule, tracking trade volume.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str664Key)]
	[DescriptionLoc(LocalizedStrings.Str881Key)]
	public class RiskTradeVolumeRule : RiskRule
	{
		private decimal _volume;

		/// <summary>
		/// Trade volume.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str882Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Volume
		{
			get => _volume;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_volume = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (!execMsg.HasTradeInfo())
				return false;

			return execMsg.TradeVolume >= Volume;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Volume), Volume);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Volume = storage.GetValue<decimal>(nameof(Volume));
		}
	}

	/// <summary>
	/// Risk-rule, tracking orders execution frequency.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str883Key)]
	[DescriptionLoc(LocalizedStrings.Str884Key)]
	public class RiskTradeFreqRule : RiskRule
	{
		private DateTimeOffset? _endTime;
		private int _current;

		private void UpdateTitle()
		{
			Title = Count + " -> " + Interval;
		}

		private int _count;

		/// <summary>
		/// Number of trades.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str878Key)]
		[DescriptionLoc(LocalizedStrings.Str232Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get => _count;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_count = value;
				UpdateTitle();
			}
		}

		private TimeSpan _interval;

		/// <summary>
		/// Interval, during which trades quantity will be monitored.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str175Key)]
		[DescriptionLoc(LocalizedStrings.Str885Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_interval = value;
				UpdateTitle();
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_current = 0;
			_endTime = null;
		}

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (!execMsg.HasTradeInfo())
				return false;

			var time = message.LocalTime;

			if (time.IsDefault())
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

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Count), Count);
			storage.SetValue(nameof(Interval), Interval);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>(nameof(Count));
			Interval = storage.GetValue<TimeSpan>(nameof(Interval));
		}
	}
}