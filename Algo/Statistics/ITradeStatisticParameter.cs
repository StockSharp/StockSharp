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

	using Ecng.Serialization;

	using StockSharp.Algo.PnL;
	using StockSharp.Localization;

	/// <summary>
	/// The interface, describing statistic parameter, calculated based on trade.
	/// </summary>
	public interface ITradeStatisticParameter
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
	[DisplayNameLoc(LocalizedStrings.Str983Key)]
	[DescriptionLoc(LocalizedStrings.Str984Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class WinningTradesParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
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
	[DisplayNameLoc(LocalizedStrings.Str986Key)]
	[DescriptionLoc(LocalizedStrings.Str987Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class LossingTradesParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
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
	[DisplayNameLoc(LocalizedStrings.Str988Key)]
	[DescriptionLoc(LocalizedStrings.Str989Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class TradeCountParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
		/// <inheritdoc />
		public void Add(PnLInfo info)
		{
			Value++;
		}
	}

	/// <summary>
	/// Total number of closing trades.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str990Key)]
	[DescriptionLoc(LocalizedStrings.Str991Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class RoundtripCountParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
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
	[DisplayNameLoc(LocalizedStrings.Str992Key)]
	[DescriptionLoc(LocalizedStrings.Str993Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class AverageTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
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
	[DisplayNameLoc(LocalizedStrings.Str994Key)]
	[DescriptionLoc(LocalizedStrings.Str995Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class AverageWinTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
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
	[DisplayNameLoc(LocalizedStrings.Str996Key)]
	[DescriptionLoc(LocalizedStrings.Str997Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class AverageLossTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
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
}