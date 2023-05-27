#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Statistics.Algo
File: IPnLStatisticParameter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Statistics
{
	using System;

	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing statistic parameter, calculated based on the profit-loss value (maximal contraction, Sharp coefficient etc.).
	/// </summary>
	public interface IPnLStatisticParameter
	{
		/// <summary>
		/// To add new data to the parameter.
		/// </summary>
		/// <param name="marketTime">The exchange time.</param>
		/// <param name="pnl">The profit-loss value.</param>
		void Add(DateTimeOffset marketTime, decimal pnl);
	}

	/// <summary>
	/// The maximal profit value for the entire period.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str958Key)]
	[DescriptionLoc(LocalizedStrings.Str959Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class MaxProfitParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="MaxProfitParameter"/>.
		/// </summary>
		public MaxProfitParameter()
			: base(StatisticParameterTypes.MaxProfit)
		{
		}

		/// <inheritdoc />
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			Value = Math.Max(Value, pnl);
		}
	}

	/// <summary>
	/// Maximum absolute drawdown during the whole period.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str960Key)]
	[DescriptionLoc(LocalizedStrings.Str961Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class MaxDrawdownParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="MaxDrawdownParameter"/>.
		/// </summary>
		public MaxDrawdownParameter()
			: base(StatisticParameterTypes.MaxDrawdown)
		{
		}

		private decimal _maxEquity = decimal.MinValue;

		/// <inheritdoc />
		public override void Reset()
		{
			_maxEquity = decimal.MinValue;
			base.Reset();
		}

		/// <inheritdoc />
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			_maxEquity = Math.Max(_maxEquity, pnl);
			Value = Math.Max(Value, _maxEquity - pnl);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("MaxEquity", _maxEquity);
			base.Save(storage);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			_maxEquity = storage.GetValue<decimal>("MaxEquity");
			base.Load(storage);
		}
	}

	/// <summary>
	/// Maximum relative equity drawdown during the whole period.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str962Key)]
	[DescriptionLoc(LocalizedStrings.Str963Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class MaxRelativeDrawdownParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="MaxRelativeDrawdownParameter"/>.
		/// </summary>
		public MaxRelativeDrawdownParameter()
			: base(StatisticParameterTypes.MaxRelativeDrawdown)
		{
		}

		private decimal _maxEquity = decimal.MinValue;

		/// <inheritdoc />
		public override void Reset()
		{
			_maxEquity = decimal.MinValue;
			base.Reset();
		}

		/// <inheritdoc />
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			_maxEquity = Math.Max(_maxEquity, pnl);

			var drawdown = _maxEquity - pnl;
			Value = Math.Max(Value, _maxEquity != 0 ? drawdown / _maxEquity : 0);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("MaxEquity", _maxEquity);
			base.Save(storage);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			_maxEquity = storage.GetValue<decimal>("MaxEquity");
			base.Load(storage);
		}
	}

	/// <summary>
	/// Relative income for the whole time period.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str964Key)]
	[DescriptionLoc(LocalizedStrings.Str965Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class ReturnParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="ReturnParameter"/>.
		/// </summary>
		public ReturnParameter()
			: base(StatisticParameterTypes.Return)
		{
		}

		private decimal _minEquity = decimal.MaxValue;

		/// <inheritdoc />
		public override void Reset()
		{
			_minEquity = decimal.MaxValue;
			base.Reset();
		}

		/// <inheritdoc />
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			_minEquity = Math.Min(_minEquity, pnl);

			var profit = pnl - _minEquity;
			Value = Math.Max(Value, _minEquity != 0 ? profit / _minEquity : 0);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("MinEquity", _minEquity);
			base.Save(storage);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			_minEquity = storage.GetValue<decimal>("MinEquity");
			base.Load(storage);
		}
	}

	/// <summary>
	/// Recovery factor (net profit / maximum drawdown).
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str966Key)]
	[DescriptionLoc(LocalizedStrings.Str967Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class RecoveryFactorParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		private readonly MaxDrawdownParameter _maxDrawdown;
		private readonly NetProfitParameter _netProfit;

		/// <summary>
		/// Initialize <see cref="RecoveryFactorParameter"/>.
		/// </summary>
		/// <param name="maxDrawdown"><see cref="MaxDrawdownParameter"/></param>
		/// <param name="netProfit"><see cref="NetProfitParameter"/></param>
		public RecoveryFactorParameter(MaxDrawdownParameter maxDrawdown, NetProfitParameter netProfit)
			: base(StatisticParameterTypes.RecoveryFactor)
		{
			_maxDrawdown = maxDrawdown ?? throw new ArgumentNullException(nameof(maxDrawdown));
			_netProfit = netProfit ?? throw new ArgumentNullException(nameof(netProfit));
		}

		/// <inheritdoc />
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			Value = _maxDrawdown.Value != 0 ? _netProfit.Value / _maxDrawdown.Value : 0;
		}
	}

	/// <summary>
	/// Net profit for whole time period.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str968Key)]
	[DescriptionLoc(LocalizedStrings.Str969Key)]
	[CategoryLoc(LocalizedStrings.PnLKey)]
	public class NetProfitParameter : BaseStatisticParameter<decimal>, IPnLStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="NetProfitParameter"/>.
		/// </summary>
		public NetProfitParameter()
			: base(StatisticParameterTypes.NetProfit)
		{
		}

		private decimal? _firstPnL;

		/// <inheritdoc />
		public override void Reset()
		{
			_firstPnL = null;
			base.Reset();
		}

		/// <inheritdoc />
		public void Add(DateTimeOffset marketTime, decimal pnl)
		{
			if (_firstPnL == null)
				_firstPnL = pnl;

			Value = pnl - _firstPnL.Value;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("FirstPnL", _firstPnL);
			base.Save(storage);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			_firstPnL = storage.GetValue<decimal?>("FirstPnL");
			base.Load(storage);
		}
	}
}