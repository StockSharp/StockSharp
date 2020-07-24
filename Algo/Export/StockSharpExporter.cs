#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Algo
File: BinExporter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.Messages;

	/// <summary>
	/// The export into the StockSharp format.
	/// </summary>
	public class StockSharpExporter : BaseExporter
	{
		private readonly IStorageRegistry _storageRegistry;
		private readonly IMarketDataDrive _drive;
		private readonly StorageFormats _format;

		/// <summary>
		/// Initializes a new instance of the <see cref="StockSharpExporter"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		/// <param name="drive">Storage.</param>
		/// <param name="format">Format type.</param>
		public StockSharpExporter(DataType dataType, Func<int, bool> isCancelled, IStorageRegistry storageRegistry, IMarketDataDrive drive, StorageFormats format)
			: base(dataType, isCancelled, drive.CheckOnNull().Path)
		{
			_storageRegistry = storageRegistry ?? throw new ArgumentNullException(nameof(storageRegistry));
			_drive = drive ?? throw new ArgumentNullException(nameof(drive));
			_format = format;
		}

		private int _batchSize = 50;

		/// <summary>
		/// The size of transmitted data package. The default is 50 elements.
		/// </summary>
		public int BatchSize
		{
			get => _batchSize;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_batchSize = value;
			}
		}

		private (int, DateTimeOffset?) Export(IEnumerable<Message> messages)
		{
			var count = 0;
			var lastTime = default(DateTimeOffset?);

			foreach (var batch in messages.Batch(BatchSize))
			{
				foreach (var group in batch.GroupBy(m => m.TryGetSecurityId()))
				{
					var b = group.ToArray();

					var storage = _storageRegistry.GetStorage(group.Key ?? default, DataType.MessageType, DataType.Arg, _drive, _format);

					if (!CanProcess(b.Length))
						break;

					storage.Save(b);

					count += b.Length;

					if (b.LastOrDefault() is IServerTimeMessage timeMsg)
						lastTime = timeMsg.ServerTime;
				}
			}

			return (count, lastTime);
		}

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) ExportOrderLog(IEnumerable<ExecutionMessage> messages)
			=> Export(messages);

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) ExportTicks(IEnumerable<ExecutionMessage> messages)
			=> Export(messages);

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) ExportTransactions(IEnumerable<ExecutionMessage> messages)
			=> Export(messages);

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<QuoteChangeMessage> messages)
			=> Export(messages);

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<Level1ChangeMessage> messages)
			=> Export(messages);

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<PositionChangeMessage> messages)
			=> Export(messages);

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<IndicatorValue> values) => throw new NotSupportedException();

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<CandleMessage> messages)
			=> Export(messages);

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<NewsMessage> messages)
			=> Export(messages);

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<SecurityMessage> messages) => throw new NotSupportedException();
	}
}