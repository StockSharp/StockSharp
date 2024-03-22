#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Commissions.Algo
File: CommissionRule.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Commissions
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The commission calculating rule.
	/// </summary>
	[DataContract]
	public abstract class CommissionRule : NotifiableObject, ICommissionRule
	{
		/// <summary>
		/// Initialize <see cref="CommissionRule"/>.
		/// </summary>
		protected CommissionRule()
		{
			UpdateTitle();
		}

		private Unit _value = new();

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CommissionKey,
			Description = LocalizedStrings.CommissionValueKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public Unit Value
		{
			get => _value;
			set
			{
				_value = value ?? throw new ArgumentNullException(nameof(value));
				NotifyChanged();
			}
		}

		/// <summary>
		/// Get title.
		/// </summary>
		protected virtual string GetTitle() => string.Empty;

		/// <summary>
		/// Update title.
		/// </summary>
		protected void UpdateTitle() => Title = GetTitle();

		private string _title;

		/// <inheritdoc />
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

		/// <inheritdoc />
		public virtual void Reset()
		{
		}

		/// <inheritdoc />
		public abstract decimal? Process(ExecutionMessage message);

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Value = storage.GetValue<Unit>(nameof(Value));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Value), Value);
		}

		/// <summary>
		/// Get result value.
		/// </summary>
		/// <param name="baseValue">Base value.</param>
		/// <returns>Result value.</returns>
		protected decimal? GetValue(decimal? baseValue)
		{
			if (baseValue == null)
				return null;

			if (Value.Type == UnitTypes.Percent)
				return (baseValue.Value * Value.Value) / 100m;

			return (decimal)Value;
		}
	}

	/// <summary>
	/// Order commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderKey,
		Description = LocalizedStrings.OrderCommissionKey,
		GroupName = LocalizedStrings.OrdersKey)]
	public class CommissionPerOrderRule : CommissionRule
	{
		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (message.HasOrderInfo())
				return GetValue(message.OrderPrice);

			return null;
		}
	}

	/// <summary>
	/// Trade commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradeKey,
		Description = LocalizedStrings.TradeCommissionKey,
		GroupName = LocalizedStrings.TradesKey)]
	public class CommissionPerTradeRule : CommissionRule
	{
		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (message.HasTradeInfo())
				return GetValue(message.TradePrice);

			return null;
		}
	}

	/// <summary>
	/// Order volume commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderVolume2Key,
		Description = LocalizedStrings.OrderVolCommissionKey,
		GroupName = LocalizedStrings.OrdersKey)]
	public class CommissionPerOrderVolumeRule : CommissionRule
	{
		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (message.HasOrderInfo())
				return (decimal)(message.OrderVolume * Value);

			return null;
		}
	}

	/// <summary>
	/// Trade volume commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradeVolumeKey,
		Description = LocalizedStrings.TradeVolCommissionKey,
		GroupName = LocalizedStrings.TradesKey)]
	public class CommissionPerTradeVolumeRule : CommissionRule
	{
		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (message.HasTradeInfo())
				return (decimal)(message.TradeVolume * Value);

			return null;
		}
	}

	/// <summary>
	/// Number of orders commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderCountKey,
		Description = LocalizedStrings.OrderCountCommissionKey,
		GroupName = LocalizedStrings.OrdersKey)]
	public class CommissionPerOrderCountRule : CommissionRule
	{
		private int _currentCount;
		private int _count;

		/// <summary>
		/// Order count.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.OrdersKey,
			Description = LocalizedStrings.OrdersCountKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public int Count
		{
			get => _count;
			set
			{
				_count = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _count.To<string>();

		/// <inheritdoc />
		public override void Reset()
		{
			_currentCount = 0;
			base.Reset();
		}

		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (!message.HasOrderInfo())
				return null;

			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
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
	/// Number of trades commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradesCountKey,
		Description = LocalizedStrings.TradesCountCommissionKey,
		GroupName = LocalizedStrings.TradesKey)]
	public class CommissionPerTradeCountRule : CommissionRule
	{
		private int _currentCount;
		private int _count;

		/// <summary>
		/// Number of trades.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TradesOfKey,
			Description = LocalizedStrings.LimitOrderTifKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public int Count
		{
			get => _count;
			set
			{
				_count = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _count.To<string>();

		/// <inheritdoc />
		public override void Reset()
		{
			_currentCount = 0;
			base.Reset();
		}

		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (!message.HasTradeInfo())
				return null;

			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
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
	/// Trade price commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradePriceKey,
		Description = LocalizedStrings.TradePriceCommissionKey,
		GroupName = LocalizedStrings.TradesKey)]
	public class CommissionPerTradePriceRule : CommissionRule
	{
		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (message.HasTradeInfo())
				return (decimal)(message.TradePrice * message.TradeVolume * Value);

			return null;
		}
	}

	/// <summary>
	/// Security commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityKey,
		Description = LocalizedStrings.SecurityCommissionKey,
		GroupName = LocalizedStrings.SecuritiesKey)]
	public class CommissionSecurityIdRule : CommissionRule
	{
		private SecurityId? _securityId;
		private Security _security;

		/// <summary>
		/// Security ID.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SecurityIdKey,
			Description = LocalizedStrings.SecurityIdKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public Security Security
		{
			get => _security;
			set
			{
				_security = value;
				_securityId = _security?.ToSecurityId();
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _securityId?.ToStringId();

		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (message.HasTradeInfo() && message.SecurityId == _securityId)
				return GetValue(message.TradePrice);

			return null;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			if (_securityId != null)
				storage.SetValue(nameof(Security), _securityId.Value.ToStringId());
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			if (storage.Contains(nameof(Security)) && ServicesRegistry.TrySecurityProvider is not null)
				Security = ServicesRegistry.SecurityProvider.LookupByStringId(storage.GetValue<string>(nameof(Security)));
		}
	}

	/// <summary>
	/// Security type commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityTypeKey,
		Description = LocalizedStrings.SecurityTypeCommissionKey,
		GroupName = LocalizedStrings.SecuritiesKey)]
	public class CommissionSecurityTypeRule : CommissionRule
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommissionSecurityTypeRule"/>.
		/// </summary>
		public CommissionSecurityTypeRule()
		{
			SecurityType = SecurityTypes.Stock;
		}

		private SecurityTypes _securityType;

		/// <summary>
		/// Security type.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TypeKey,
			Description = LocalizedStrings.SecurityTypeDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public SecurityTypes SecurityType
		{
			get => _securityType;
			set
			{
				_securityType = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _securityType.ToString();

		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			// TODO
			//if (message.HasTradeInfo() && message.SecurityId.SecurityType == SecurityType)
			//	return GetValue(message.TradePrice);

			return null;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(SecurityType), SecurityType);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			SecurityType = storage.GetValue<SecurityTypes>(nameof(SecurityType));
		}
	}

	/// <summary>
	/// Board commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BoardKey,
		Description = LocalizedStrings.BoardCommissionKey,
		GroupName = LocalizedStrings.BoardKey)]
	public class CommissionBoardCodeRule : CommissionRule
	{
		private ExchangeBoard _board;

		/// <summary>
		/// Board code.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BoardKey,
			Description = LocalizedStrings.BoardCodeKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public ExchangeBoard Board
		{
			get => _board;
			set
			{
				_board = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _board?.Code;

		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (message.HasTradeInfo() && Board != null && message.SecurityId.BoardCode.EqualsIgnoreCase(Board.Code))
				return GetValue(message.TradePrice);

			return null;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			if (Board != null)
				storage.SetValue(nameof(Board), Board.Code);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			var boardCode = storage.GetValue<string>(nameof(Board));

			if (!boardCode.IsEmpty())
				Board = ServicesRegistry.TryExchangeInfoProvider?.TryGetExchangeBoard(boardCode);
		}
	}

	/// <summary>
	/// Turnover commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TurnoverKey,
		Description = LocalizedStrings.TurnoverCommissionKey,
		GroupName = LocalizedStrings.TradesKey)]
	public class CommissionTurnOverRule : CommissionRule
	{
		private decimal _currentTurnOver;
		private decimal _turnOver;

		/// <summary>
		/// Turnover.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TurnoverKey,
			Description = LocalizedStrings.TurnoverKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public decimal TurnOver
		{
			get => _turnOver;
			set
			{
				_turnOver = value;
				UpdateTitle();
			}
		}

		/// <inheritdoc />
		protected override string GetTitle() => _turnOver.To<string>();

		/// <inheritdoc />
		public override void Reset()
		{
			_turnOver = 0;
			base.Reset();
		}

		/// <inheritdoc />
		public override decimal? Process(ExecutionMessage message)
		{
			if (!message.HasTradeInfo())
				return null;

			_currentTurnOver += message.GetTradePrice() * message.SafeGetVolume();

			if (_currentTurnOver < TurnOver)
				return null;

			return (decimal)Value;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(TurnOver), TurnOver);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			TurnOver = storage.GetValue<decimal>(nameof(TurnOver));
		}
	}
}