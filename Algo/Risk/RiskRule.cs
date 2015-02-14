namespace StockSharp.Algo.Risk
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Collections;

	using StockSharp.Messages;

	using StockSharp.Localization;

	/// <summary>
	/// Базовое риск-правило.
	/// </summary>
	public abstract class RiskRule : NotifiableObject, IRiskRule
	{
		/// <summary>
		/// Инициализировать <see cref="RiskRule"/>.
		/// </summary>
		protected RiskRule()
		{
		}

		private string _title;

		/// <summary>
		/// Заголовок.
		/// </summary>
		[Browsable(false)]
		public string Title
		{
			get { return _title; }
			protected set
			{
				_title = value;
				NotifyChanged("Title");
			}
		}

		/// <summary>
		/// Действие, которое необходимо совершить в случае активации правило.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str722Key)]
		[DescriptionLoc(LocalizedStrings.Str859Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RiskActions Action { get; set; }

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		public virtual void Reset()
		{
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
		public abstract bool ProcessMessage(Message message);

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Action = storage.GetValue<RiskActions>("Action");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("Action", Action.To<string>());
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее прибыль-убыток.
	/// </summary>
	[DisplayName("P&L")]
	[DescriptionLoc(LocalizedStrings.Str860Key)]
	public class RiskPnLRule : RiskRule
	{
		private decimal _pnL;

		/// <summary>
		/// Прибыль-убыток.
		/// </summary>
		[DisplayName("P&L")]
		[DescriptionLoc(LocalizedStrings.Str861Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal PnL
		{
			get { return _pnL; }
			set
			{
				_pnL = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
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
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("PnL", PnL);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			PnL = storage.GetValue<decimal>("PnL");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее размер позиции.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str862Key)]
	[DescriptionLoc(LocalizedStrings.Str863Key)]
	public class RiskPositionSizeRule : RiskRule
	{
		private decimal _position;

		/// <summary>
		/// Размер позиции.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str862Key)]
		[DescriptionLoc(LocalizedStrings.Str864Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Position
		{
			get { return _position; }
			set
			{
				_position = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
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
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Position", Position);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Position = storage.GetValue<decimal>("Position");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее время жизни позиции.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str865Key)]
	[DescriptionLoc(LocalizedStrings.Str866Key)]
	public class RiskPositionTimeRule : RiskRule
	{
		private readonly Dictionary<Tuple<SecurityId, string>, DateTime> _posOpenTime = new Dictionary<Tuple<SecurityId, string>, DateTime>();
		private TimeSpan _time;

		/// <summary>
		/// Время жизни позиции.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str219Key)]
		[DescriptionLoc(LocalizedStrings.Str867Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public TimeSpan Time
		{
			get { return _time; }
			set
			{
				_time = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_posOpenTime.Clear();
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
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

					if (removingPos != null)
						removingPos.ForEach(t => _posOpenTime.Remove(t));

					return removingPos != null;
				}
			}

			return false;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Time", Time);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Time = storage.GetValue<TimeSpan>("Time");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее размер комиссии.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str159Key)]
	[DescriptionLoc(LocalizedStrings.Str868Key)]
	public class RiskCommissionRule : RiskRule
	{
		private decimal _commission;

		/// <summary>
		/// Размер комиссии.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str869Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Commission
		{
			get { return _commission; }
			set
			{
				_commission = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
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
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Commission", Commission);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Commission = storage.GetValue<decimal>("Commission");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее размер проскальзывания.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str163Key)]
	[DescriptionLoc(LocalizedStrings.Str870Key)]
	public class RiskSlippageRule : RiskRule
	{
		private decimal _slippage;

		/// <summary>
		/// Размер проскальзывания.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str871Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Slippage
		{
			get { return _slippage; }
			set
			{
				_slippage = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
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
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Slippage", Slippage);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Slippage = storage.GetValue<decimal>("Slippage");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее цену заявки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str872Key)]
	[DescriptionLoc(LocalizedStrings.Str873Key)]
	public class RiskOrderPriceRule : RiskRule
	{
		private decimal _price;

		/// <summary>
		/// Цена заявки.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.OrderPriceKey)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Price
		{
			get { return _price; }
			set
			{
				_price = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
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
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Price", Price);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Price = storage.GetValue<decimal>("Price");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее объем заявки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str662Key)]
	[DescriptionLoc(LocalizedStrings.Str874Key)]
	public class RiskOrderVolumeRule : RiskRule
	{
		private decimal _volume;

		/// <summary>
		/// Объем заявки.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str875Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Volume
		{
			get { return _volume; }
			set
			{
				_volume = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
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
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Volume", Volume);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Volume = storage.GetValue<decimal>("Volume");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее частоту выставления заявок.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str876Key)]
	[DescriptionLoc(LocalizedStrings.Str877Key)]
	public class RiskOrderFreqRule : RiskRule
	{
		private DateTime? _endTime;
		private int _current;

		private int _count;

		/// <summary>
		/// Количество заявок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str878Key)]
		[DescriptionLoc(LocalizedStrings.Str669Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get { return _count; }
			set
			{
				_count = value;
				Title = value.To<string>();
			}
		}

		private TimeSpan _interval;


		/// <summary>
		/// Интервал, в пределах которого будет отслеживать количество заявок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str175Key)]
		[DescriptionLoc(LocalizedStrings.Str879Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public TimeSpan Interval
		{
			get { return _interval; }
			set
			{
				_interval = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_current = 0;
			_endTime = null;
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
		public override bool ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderPairReplace:
				{
					if (_endTime == null)
					{
						_endTime = message.LocalTime + Interval;
						_current = 1;
						return false;
					}

					if (message.LocalTime < _endTime)
					{
						_current++;

						if (_current >= Count)
						{
							_endTime = message.LocalTime + Interval;
							_current = 0;
							return true;
						}

						return false;
					}

					_endTime = message.LocalTime + Interval;
					_current = 0;
					return false;
				}
			}

			return false;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Count", Count);
			storage.SetValue("Interval", Interval);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>("Count");
			Interval = storage.GetValue<TimeSpan>("Interval");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее цену сделки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str672Key)]
	[DescriptionLoc(LocalizedStrings.Str880Key)]
	public class RiskTradePriceRule : RiskRule
	{
		private decimal _price;

		/// <summary>
		/// Цена сделки.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str147Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Price
		{
			get { return _price; }
			set
			{
				_price = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (execMsg.ExecutionType != ExecutionTypes.Trade)
				return false;

			return execMsg.TradePrice >= Price;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Price", Price);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Price = storage.GetValue<decimal>("Price");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее объем сделки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str664Key)]
	[DescriptionLoc(LocalizedStrings.Str881Key)]
	public class RiskTradeVolumeRule : RiskRule
	{
		private decimal _volume;

		/// <summary>
		/// Объем сделки.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str882Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Volume
		{
			get { return _volume; }
			set
			{
				_volume = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (execMsg.ExecutionType != ExecutionTypes.Trade)
				return false;

			return execMsg.Volume >= Volume;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Volume", Volume);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Volume = storage.GetValue<decimal>("Volume");
		}
	}

	/// <summary>
	/// Риск-правило, отслеживающее частоту совершения сделок.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str883Key)]
	[DescriptionLoc(LocalizedStrings.Str884Key)]
	public class RiskTradeFreqRule : RiskRule
	{
		private DateTime? _endTime;
		private int _current;

		private int _count;

		/// <summary>
		/// Количество сделок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str878Key)]
		[DescriptionLoc(LocalizedStrings.Str232Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get { return _count; }
			set
			{
				_count = value;
				Title = value.To<string>();
			}
		}

		private TimeSpan _interval;

		/// <summary>
		/// Интервал, в пределах которого будет отслеживать количество сделок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str175Key)]
		[DescriptionLoc(LocalizedStrings.Str885Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public TimeSpan Interval
		{
			get { return _interval; }
			set
			{
				_interval = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_current = 0;
			_endTime = null;
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
		public override bool ProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return false;

			var execMsg = (ExecutionMessage)message;

			if (execMsg.ExecutionType != ExecutionTypes.Trade)
				return false;

			if (_endTime == null)
			{
				_endTime = message.LocalTime + Interval;
				_current = 1;
				return false;
			}

			if (message.LocalTime < _endTime)
			{
				_current++;

				if (_current >= Count)
				{
					_endTime = message.LocalTime + Interval;
					_current = 0;
					return true;
				}

				return false;
			}

			_endTime = message.LocalTime + Interval;
			_current = 0;
			return false;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Count", Count);
			storage.SetValue("Interval", Interval);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>("Count");
			Interval = storage.GetValue<TimeSpan>("Interval");
		}
	}
}