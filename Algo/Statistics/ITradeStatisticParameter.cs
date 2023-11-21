#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Statistics.Algo
File: ITradeStatisticParameter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Statistics
{
	using System;
	using System.ComponentModel.DataAnnotations;
	
	using Ecng.Serialization;

	using StockSharp.Algo.PnL;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing statistic parameter, calculated based on trade.
	/// </summary>
	public interface ITradeStatisticParameter : IStatisticParameter
	{
		/// <summary>
		/// To add information about new trade to the parameter.
		/// </summary>
		/// <param name="info">Information on new trade.</param>
		void Add(PnLInfo info);
	}

	/// <summary>
	/// Number of trades won (whose profit is greater than 0).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ProfitTradesKey,
		Description = LocalizedStrings.ProfitTradesDescKey,
		GroupName = LocalizedStrings.TradesKey,
		Order = 100
	)]
	public class WinningTradesParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="WinningTradesParameter"/>.
		/// </summary>
		public WinningTradesParameter()
			: base(StatisticParameterTypes.WinningTrades)
        {
        }

        /// <inheritdoc />
        public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			if (info.PnL <= 0)
				return;

			Value++;
		}
	}

	/// <summary>
	/// Number of trades lost with zero profit (whose profit is less than or equal to 0).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LossTradesKey,
		Description = LocalizedStrings.LossTradesDescKey,
		GroupName = LocalizedStrings.TradesKey,
		Order = 101
	)]
	public class LossingTradesParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="LossingTradesParameter"/>.
		/// </summary>
		public LossingTradesParameter()
			: base(StatisticParameterTypes.LossingTrades)
		{
		}

		/// <inheritdoc />
		public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			if (info.ClosedVolume > 0 && info.PnL <= 0)
				Value++;
		}
	}

	/// <summary>
	/// Total number of trades.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TotalTradesKey,
		Description = LocalizedStrings.TotalTradesDescKey,
		GroupName = LocalizedStrings.TradesKey,
		Order = 102
	)]
	public class TradeCountParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="TradeCountParameter"/>.
		/// </summary>
		public TradeCountParameter()
			: base(StatisticParameterTypes.TradeCount)
		{
		}

		/// <inheritdoc />
		public void Add(PnLInfo info)
		{
			Value++;
		}
	}

	/// <summary>
	/// Total number of closing trades.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClosingTradesKey,
		Description = LocalizedStrings.ClosingTradesDescKey,
		GroupName = LocalizedStrings.TradesKey,
		Order = 103
	)]
	public class RoundtripCountParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="RoundtripCountParameter"/>.
		/// </summary>
		public RoundtripCountParameter()
			: base(StatisticParameterTypes.RoundtripCount)
		{
		}

		/// <inheritdoc />
		public void Add(PnLInfo info)
		{
			if (info.ClosedVolume > 0)
				Value++;
		}
	}

	/// <summary>
	/// Average trade profit.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AverageProfitKey,
		Description = LocalizedStrings.AverageTradeProfitKey,
		GroupName = LocalizedStrings.TradesKey,
		Order = 104
	)]
	public class AverageTradeProfitParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="AverageTradeProfitParameter"/>.
		/// </summary>
		public AverageTradeProfitParameter()
			: base(StatisticParameterTypes.AverageTradeProfit)
		{
		}

		private decimal _sum;
		private int _count;

		/// <inheritdoc />
		public override void Reset()
		{
			_sum = 0;
			_count = 0;
			base.Reset();
		}

		/// <inheritdoc />
		public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			if (info.ClosedVolume == 0)
				return;

			_sum += info.PnL;
			_count++;

			Value = _count > 0 ? _sum / _count : 0;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Sum", _sum);
			storage.SetValue("Count", _count);

			base.Save(storage);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			_sum = storage.GetValue<decimal>("Sum");
			_count = storage.GetValue<int>("Count");

			base.Load(storage);
		}
	}

	/// <summary>
	/// Average winning trade.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AverageWinKey,
		Description = LocalizedStrings.AverageWinTradeKey,
		GroupName = LocalizedStrings.TradesKey,
		Order = 105
	)]
	public class AverageWinTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="AverageWinTradeParameter"/>.
		/// </summary>
		public AverageWinTradeParameter()
			: base(StatisticParameterTypes.AverageWinTrades)
		{
		}

		private decimal _sum;
		private int _count;

		/// <inheritdoc />
		public override void Reset()
		{
			_sum = 0;
			_count = 0;
			base.Reset();
		}

		/// <inheritdoc />
		public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			if (info.ClosedVolume == 0)
				return;

			if (info.PnL > 0)
			{
				_sum += info.PnL;
				_count++;
			}

			Value = _count > 0 ? _sum / _count : 0;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Sum", _sum);
			storage.SetValue("Count", _count);

			base.Save(storage);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			_sum = storage.GetValue<decimal>("Sum");
			_count = storage.GetValue<int>("Count");

			base.Load(storage);
		}
	}

	/// <summary>
	/// Average losing trade.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AverageLossKey,
		Description = LocalizedStrings.AverageLossTradeKey,
		GroupName = LocalizedStrings.TradesKey,
		Order = 106
	)]
	public class AverageLossTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
		/// <summary>
		/// Initialize <see cref="AverageLossTradeParameter"/>.
		/// </summary>
		public AverageLossTradeParameter()
			: base(StatisticParameterTypes.AverageLossTrades)
		{
		}

		private decimal _sum;
		private int _count;

		/// <inheritdoc />
		public override void Reset()
		{
			_sum = 0;
			_count = 0;
			base.Reset();
		}

		/// <inheritdoc />
		public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			if (info.ClosedVolume == 0)
				return;

			if (info.PnL <= 0)
			{
				_sum += info.PnL;
				_count++;
			}

			Value = _count > 0 ? _sum / _count : 0;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Sum", _sum);
			storage.SetValue("Count", _count);

			base.Save(storage);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			_sum = storage.GetValue<decimal>("Sum");
			_count = storage.GetValue<int>("Count");

			base.Load(storage);
		}
	}

	/// <summary>
	/// Average trades count per one base.
	/// </summary>
	public abstract class PerBaseTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
		private DateTime _currStart;
		private int _currCount;

		private int _periodsCount;

		/// <summary>
		/// Initialize <see cref="PerMonthTradeParameter"/>.
		/// </summary>
		/// <param name="type"><see cref="IStatisticParameter.Type"/></param>
		protected PerBaseTradeParameter(StatisticParameterTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		public override void Reset()
		{
			_currStart = default;
			_currCount = default;
			_periodsCount = default;

			base.Reset();
		}

		/// <summary>
		/// Align the specified date for exact period start.
		/// </summary>
		/// <param name="date">Trade date.</param>
		/// <returns>Aligned value.</returns>
		protected abstract DateTime Align(DateTime date);

		/// <inheritdoc />
		public void Add(PnLInfo info)
		{
			if (info is null)
				throw new ArgumentNullException(nameof(info));

			var date = Align(info.ServerTime.UtcDateTime);

			if (_currStart == default)
			{
				_currStart = date;

				_periodsCount = 1;
				_currCount = 1;

				Value = _currCount;
			}
			else if (_currStart == date)
			{
				Value = ((Value * _periodsCount - _currCount) + ++_currCount) / _periodsCount;
			}
			else
			{
				_currStart = date;

				_currCount = 1;

				Value = (Value * _periodsCount + _currCount) / ++_periodsCount;
			}
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage
				.Set("CurrStart", _currStart)
				.Set("PeriodsCount", _periodsCount)
				.Set("CurrCount", _currCount)
			;

			base.Save(storage);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			_currStart = storage.GetValue<DateTime>("CurrStart");
			_periodsCount = storage.GetValue<int>("PeriodsCount");
			_currCount = storage.GetValue<int>("CurrCount");

			base.Load(storage);
		}
	}

	/// <summary>
	/// Average trades count per one month.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PerMonthTradesKey,
		Description = LocalizedStrings.PerMonthTradesDescKey,
		GroupName = LocalizedStrings.TradesKey,
		Order = 107)]	
	public class PerMonthTradeParameter : PerBaseTradeParameter
	{
		/// <summary>
		/// Initialize <see cref="PerMonthTradeParameter"/>.
		/// </summary>
		public PerMonthTradeParameter()
			: base(StatisticParameterTypes.PerMonthTrades)
        {
        }

		/// <inheritdoc/>
		protected override DateTime Align(DateTime date) => new(date.Year, date.Month, 1);
	}

	/// <summary>
	/// Average trades count per one day.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PerDayTradesKey,
		Description = LocalizedStrings.PerDayTradesDescKey,
		GroupName = LocalizedStrings.TradesKey,
		Order = 108)]
	public class PerDayTradeParameter : PerBaseTradeParameter
	{
		/// <summary>
		/// Initialize <see cref="PerDayTradeParameter"/>.
		/// </summary>
		public PerDayTradeParameter()
			: base(StatisticParameterTypes.PerDayTrades)
		{
		}

		/// <inheritdoc/>
		protected override DateTime Align(DateTime date) => date.Date;
	}
}