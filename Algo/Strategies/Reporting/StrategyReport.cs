#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Reporting.Algo
File: StrategyReport.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies.Reporting
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Strategies;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The base report generator for strategies.
	/// </summary>
	public abstract class StrategyReport
	{
		/// <summary>
		/// Initialize <see cref="StrategyReport"/>.
		/// </summary>
		/// <param name="strategies">Strategies, requiring the report generation.</param>
		/// <param name="fileName">The name of the file, in which the report is generated.</param>
		protected StrategyReport(IEnumerable<Strategy> strategies, string fileName)
		{
			if (strategies == null)
				throw new ArgumentNullException(nameof(strategies));

			if (strategies.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(strategies));

			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			Strategies = strategies;
			FileName = fileName;
		}

		/// <summary>
		/// The name of the file, in which the report is generated.
		/// </summary>
		public string FileName { get; }

		/// <summary>
		/// Strategies, requiring the report generation.
		/// </summary>
		public IEnumerable<Strategy> Strategies { get; }

		/// <summary>
		/// To generate the report.
		/// </summary>
		public abstract void Generate();

		/// <summary>
		/// To format the date in string.
		/// </summary>
		/// <param name="time">Time.</param>
		/// <returns>The formatted string.</returns>
		protected virtual string Format(TimeSpan? time)
		{
			return time == null
				? string.Empty
				: "{0:00}:{1:00}:{2:00}".Put(time.Value.TotalHours, time.Value.Minutes, time.Value.Seconds);
		}

		/// <summary>
		/// To format the date in string.
		/// </summary>
		/// <param name="time">Time.</param>
		/// <returns>The formatted string.</returns>
		protected virtual string Format(DateTimeOffset time)
		{
			return time.To<string>();
		}

		/// <summary>
		/// Convert order side into string.
		/// </summary>
		/// <param name="direction">Order side.</param>
		/// <returns>The formatted string.</returns>
		protected virtual string Format(Sides direction)
		{
			return direction == Sides.Buy ? LocalizedStrings.Str403 : LocalizedStrings.Str404;
		}

		/// <summary>
		/// Convert order state into string.
		/// </summary>
		/// <param name="state">Order state.</param>
		/// <returns>The formatted string.</returns>
		protected virtual string Format(OrderStates state)
		{
			switch (state)
			{
				case OrderStates.None:
					return string.Empty;
				case OrderStates.Active:
					return LocalizedStrings.Str238;
				case OrderStates.Done:
					return LocalizedStrings.Str239;
				case OrderStates.Failed:
					return LocalizedStrings.Str152;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, LocalizedStrings.Str1219);
			}
		}

		/// <summary>
		/// To format the order type in string.
		/// </summary>
		/// <param name="type">Order type.</param>
		/// <returns>The formatted string.</returns>
		protected virtual string Format(OrderTypes? type)
		{
			switch (type)
			{
				case OrderTypes.Limit:
					return LocalizedStrings.Str1353;
				case OrderTypes.Market:
					return LocalizedStrings.Str241;
				case OrderTypes.Repo:
					return LocalizedStrings.Str243;
				case OrderTypes.Rps:
					return LocalizedStrings.Str1354;
				case OrderTypes.ExtRepo:
					return LocalizedStrings.Str244;
				case null:
					return string.Empty;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str1219);
			}
		}
	}
}