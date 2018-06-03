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

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
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
		/// <param name="security">Security.</param>
		/// <param name="arg">The data parameter.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		/// <param name="drive">Storage.</param>
		/// <param name="format">Format type.</param>
		public StockSharpExporter(Security security, object arg, Func<int, bool> isCancelled, IStorageRegistry storageRegistry, IMarketDataDrive drive, StorageFormats format)
			: base(security, arg, isCancelled, drive.Path)
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

		private void Export<TMessage>(IEnumerable<TMessage> messages)
			where TMessage : Message
		{
			Export(typeof(TMessage), messages);
		}

		private void Export(Type messageType, IEnumerable<Message> messages)
		{
			IMarketDataStorage storage = null;

			foreach (var batch in messages.Batch(BatchSize).Select(b => b.ToArray()))
			{
				if (storage == null)
					storage = _storageRegistry.GetStorage(Security, messageType, Arg, _drive, _format);

				if (!CanProcess(batch.Length))
					break;

				storage.Save(batch);
			}
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			Export(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Export(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Export(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<PositionChangeMessage> messages)
		{
			Export(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<IndicatorValue> values)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			foreach (var group in messages.GroupBy(m => m.GetType()))
			{
				Export(group.Key, group);

				if (!CanProcess())
					break;
			}
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Export(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			throw new NotSupportedException();
		}
	}
}