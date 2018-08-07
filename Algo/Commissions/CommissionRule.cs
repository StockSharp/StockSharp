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
	using System.Runtime.Serialization;
	using DataContract = System.Runtime.Serialization.DataContractAttribute;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;
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
		}

		private Unit _value = new Unit();

		/// <summary>
		/// Commission value.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.CommissionValueKey)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Unit Value
		{
			get => _value;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_value = value;
				NotifyChanged(nameof(Value));
			}
		}

		/// <summary>
		/// Total commission.
		/// </summary>
		[Browsable(false)]
		public decimal Commission { get; private set; }

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

		/// <summary>
		/// To reset the state.
		/// </summary>
		public virtual void Reset()
		{
			Commission = 0;
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		public decimal? Process(Message message)
		{
			var commission = OnProcessExecution((ExecutionMessage)message);

			if (commission != null)
				Commission += commission.Value;

			return commission;
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected abstract decimal? OnProcessExecution(ExecutionMessage message);

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Value = storage.GetValue<Unit>(nameof(Value));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Value), Value);
		}

		internal decimal? GetValue(decimal? baseValue)
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
	[DisplayNameLoc(LocalizedStrings.Str504Key)]
	[DescriptionLoc(LocalizedStrings.Str660Key)]
	public class CommissionPerOrderRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.HasOrderInfo())
				return GetValue(message.OrderPrice);
			
			return null;
		}
	}

	/// <summary>
	/// Trade commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str506Key)]
	[DescriptionLoc(LocalizedStrings.Str661Key)]
	public class CommissionPerTradeRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.HasTradeInfo())
				return GetValue(message.TradePrice);
			
			return null;
		}
	}

	/// <summary>
	/// Order volume commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str662Key)]
	[DescriptionLoc(LocalizedStrings.Str663Key)]
	public class CommissionPerOrderVolumeRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.HasOrderInfo())
				return (decimal)(message.OrderVolume * Value);
			
			return null;
		}
	}

	/// <summary>
	/// Trade volume commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str664Key)]
	[DescriptionLoc(LocalizedStrings.Str665Key)]
	public class CommissionPerTradeVolumeRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.HasTradeInfo())
				return (decimal)(message.TradeVolume * Value);
			
			return null;
		}
	}

	/// <summary>
	/// Number of orders commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str666Key)]
	[DescriptionLoc(LocalizedStrings.Str667Key)]
	public class CommissionPerOrderCountRule : CommissionRule
	{
		private int _currentCount;
		private int _count;

		/// <summary>
		/// Order count.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str668Key)]
		[DescriptionLoc(LocalizedStrings.Str957Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get => _count;
			set
			{
				_count = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public override void Reset()
		{
			_currentCount = 0;
			base.Reset();
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (!message.HasOrderInfo())
				return null;

			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Count), Count);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>(nameof(Count));
		}
	}

	/// <summary>
	/// Number of trades commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str670Key)]
	[DescriptionLoc(LocalizedStrings.Str671Key)]
	public class CommissionPerTradeCountRule : CommissionRule
	{
		private int _currentCount;
		private int _count;

		/// <summary>
		/// Number of trades.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TradesOfKey)]
		[DescriptionLoc(LocalizedStrings.Str232Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get => _count;
			set
			{
				_count = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public override void Reset()
		{
			_currentCount = 0;
			base.Reset();
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (!message.HasTradeInfo())
				return null;

			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Count), Count);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>(nameof(Count));
		}
	}

	/// <summary>
	/// Trade price commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str672Key)]
	[DescriptionLoc(LocalizedStrings.Str673Key)]
	public class CommissionPerTradePriceRule : CommissionRule
	{
		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.HasTradeInfo())
				return (decimal)(message.TradePrice * message.TradeVolume * Value);
			
			return null;
		}
	}

	/// <summary>
	/// Security commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.SecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str674Key)]
	public class CommissionSecurityIdRule : CommissionRule
	{
		private SecurityId? _securityId;
		private Security _security;

		/// <summary>
		/// Security ID.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Security Security
		{
			get => _security;
			set
			{
				_security = value;
				_securityId = _security?.ToSecurityId();
				Title = value?.ToString();
			}
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.HasTradeInfo() && message.SecurityId == _securityId)
				return GetValue(message.TradePrice);
			
			return null;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			if (Security != null)
				storage.SetValue(nameof(Security), Security);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Security = storage.GetValue<Security>(nameof(Security));
		}
	}

	/// <summary>
	/// Security type commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str675Key)]
	[DescriptionLoc(LocalizedStrings.Str676Key)]
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
		[DisplayNameLoc(LocalizedStrings.TypeKey)]
		[DescriptionLoc(LocalizedStrings.Str360Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SecurityTypes SecurityType
		{
			get => _securityType;
			set
			{
				_securityType = value;
				Title = value.ToString();
			}
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.HasTradeInfo() && message.SecurityId.SecurityType == SecurityType)
				return GetValue(message.TradePrice);
			
			return null;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(SecurityType), SecurityType);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			SecurityType = storage.GetValue<SecurityTypes>(nameof(SecurityType));
		}
	}

	/// <summary>
	/// Board commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.BoardKey)]
	[DescriptionLoc(LocalizedStrings.BoardCommissionKey)]
	public class CommissionBoardCodeRule : CommissionRule
	{
		private ExchangeBoard _board;

		/// <summary>
		/// Board code.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExchangeBoard Board
		{
			get => _board;
			set
			{
				_board = value;
				Title = value?.Code;
			}
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.HasTradeInfo() && Board != null && message.SecurityId.BoardCode.CompareIgnoreCase(Board.Code))
				return GetValue(message.TradePrice);
			
			return null;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			if (Board != null)
				storage.SetValue(nameof(Board), Board.Code);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			var boardCode = storage.GetValue<string>(nameof(Board));

			if (!boardCode.IsEmpty())
				Board = ConfigManager.TryGetService<IExchangeInfoProvider>()?.GetExchangeBoard(boardCode);
		}
	}

	/// <summary>
	/// Turnover commission.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.TurnoverKey)]
	[DescriptionLoc(LocalizedStrings.TurnoverCommissionKey)]
	public class CommissionTurnOverRule : CommissionRule
	{
		private decimal _currentTurnOver;
		private decimal _turnOver;

		/// <summary>
		/// Turnover.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TurnoverKey)]
		[DescriptionLoc(LocalizedStrings.TurnoverKey, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal TurnOver
		{
			get => _turnOver;
			set
			{
				_turnOver = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public override void Reset()
		{
			_turnOver = 0;
			base.Reset();
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (!message.HasTradeInfo())
				return null;

			_currentTurnOver += message.GetTradePrice() * message.SafeGetVolume();

			if (_currentTurnOver < TurnOver)
				return null;

			return (decimal)Value;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(TurnOver), TurnOver);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			TurnOver = storage.GetValue<decimal>(nameof(TurnOver));
		}
	}
}