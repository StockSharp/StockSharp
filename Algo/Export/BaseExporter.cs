#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Algo
File: BaseExporter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The base class of export.
	/// </summary>
	public abstract class BaseExporter
	{
		private readonly Func<int, bool> _isCancelled;

		/// <summary>
		/// Initialize <see cref="BaseExporter"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <param name="path">The path to file.</param>
		protected BaseExporter(DataType dataType, Func<int, bool> isCancelled, string path)
		{
			if (path.IsEmpty())
				throw new ArgumentNullException(nameof(path));

			DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
			_isCancelled = isCancelled ?? throw new ArgumentNullException(nameof(isCancelled));
			Path = path;
		}

		/// <summary>
		/// Data type info.
		/// </summary>
		public DataType DataType { get; }

		/// <summary>
		/// The path to file.
		/// </summary>
		protected string Path { get; }

		/// <summary>
		/// To export values.
		/// </summary>
		/// <param name="values">Value.</param>
		/// <returns>Count and last time.</returns>
		public (int, DateTimeOffset?) Export(IEnumerable values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			return CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				if (DataType == DataType.MarketDepth)
					return Export((IEnumerable<QuoteChangeMessage>)values);
				else if (DataType == DataType.Level1)
					return Export((IEnumerable<Level1ChangeMessage>)values);
				else if (DataType == DataType.Ticks)
					return ExportTicks((IEnumerable<ExecutionMessage>)values);
				else if (DataType == DataType.OrderLog)
					return ExportOrderLog((IEnumerable<ExecutionMessage>)values);
				else if (DataType == DataType.Transactions)
					return ExportTransactions((IEnumerable<ExecutionMessage>)values);
				else if (DataType.IsCandles)
					return Export((IEnumerable<CandleMessage>)values);
				else if (DataType == DataType.News)
					return Export((IEnumerable<NewsMessage>)values);
				else if (DataType == DataType.Securities)
					return Export((IEnumerable<SecurityMessage>)values);
				else if (DataType == DataType.Securities)
					return Export((IEnumerable<SecurityMessage>)values);
				else if (DataType == DataType.PositionChanges)
					return Export((IEnumerable<PositionChangeMessage>)values);
				else if (DataType == TraderHelper.IndicatorValue)
					return Export((IEnumerable<IndicatorValue>)values);
				else
					throw new ArgumentOutOfRangeException(nameof(DataType), DataType, LocalizedStrings.Str721);
			});
		}

		/// <summary>
		/// Is it possible to continue export.
		/// </summary>
		/// <param name="exported">The number of exported elements from previous call of the method.</param>
		/// <returns><see langword="true" />, if export can be continued, otherwise, <see langword="false" />.</returns>
		protected bool CanProcess(int exported = 1)
		{
			return !_isCancelled(exported);
		}

		/// <summary>
		/// To export <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) Export(IEnumerable<QuoteChangeMessage> messages);

		/// <summary>
		/// To export <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) Export(IEnumerable<Level1ChangeMessage> messages);

		/// <summary>
		/// To export <see cref="ExecutionTypes.Tick"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) ExportTicks(IEnumerable<ExecutionMessage> messages);

		/// <summary>
		/// To export <see cref="ExecutionTypes.OrderLog"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) ExportOrderLog(IEnumerable<ExecutionMessage> messages);

		/// <summary>
		/// To export <see cref="ExecutionTypes.Transaction"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) ExportTransactions(IEnumerable<ExecutionMessage> messages);

		/// <summary>
		/// To export <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) Export(IEnumerable<CandleMessage> messages);

		/// <summary>
		/// To export <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) Export(IEnumerable<NewsMessage> messages);

		/// <summary>
		/// To export <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) Export(IEnumerable<SecurityMessage> messages);

		/// <summary>
		/// To export <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) Export(IEnumerable<PositionChangeMessage> messages);

		/// <summary>
		/// To export <see cref="IndicatorValue"/>.
		/// </summary>
		/// <param name="values">Values.</param>
		/// <returns>Count and last time.</returns>
		protected abstract (int, DateTimeOffset?) Export(IEnumerable<IndicatorValue> values);
	}
}