namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Configuration;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Ёкспорт в бинарный формат StockSharp.
	/// </summary>
	public class BinExporter : BaseExporter
	{
		private readonly IMarketDataDrive _drive;

		/// <summary>
		/// —оздать <see cref="BinExporter"/>.
		/// </summary>
		/// <param name="security">»нструмент.</param>
		/// <param name="arg">ѕараметр данных.</param>
		/// <param name="isCancelled">ќбработчик, возвращающий признак прерывани€ экспорта.</param>
		/// <param name="drive">’ранилище.</param>
		public BinExporter(Security security, object arg, Func<int, bool> isCancelled, IMarketDataDrive drive)
			: base(security, arg, isCancelled, drive.Path)
		{
			if (drive == null)
				throw new ArgumentNullException("drive");

			_drive = drive;
		}

		private int _batchSize = 50;

		/// <summary>
		/// –азмер пакета передаваемых данных. ѕо-умолчанию равен 50 элементам.
		/// </summary>
		public int BatchSize
		{
			get { return _batchSize; }
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
			IMarketDataStorage<TMessage> storage = null;

			foreach (var batch in messages.Batch(BatchSize).Select(b => b.ToArray()))
			{
				if (storage == null)
				{
					storage = (IMarketDataStorage<TMessage>)ConfigManager
						.GetService<IStorageRegistry>()
						.GetStorage(Security, typeof(TMessage), Arg, _drive);
				}

				if (CanProcess(batch.Length))
					storage.Save(batch);
			}
		}

		/// <summary>
		/// Ёкспортировать <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			Export(messages);
		}

		/// <summary>
		/// Ёкспортировать <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Export(messages);
		}

		/// <summary>
		/// Ёкспортировать <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Export(messages);
		}

		/// <summary>
		/// Ёкспортировать <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			foreach (var group in messages.GroupBy(m => m.GetType()))
			{
				var storage = ConfigManager
					.GetService<IStorageRegistry>()
					.GetCandleMessageStorage(group.Key, Security, Arg, _drive);

				foreach (var candleMessages in group.Batch(BatchSize).Select(b => b.ToArray()))
				{
					if (CanProcess(candleMessages.Length))
						storage.Save(candleMessages);	
				}

				if (!CanProcess())
					break;
			}
		}

		/// <summary>
		/// Ёкспортировать <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Ёкспортировать <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			throw new NotSupportedException();
		}
	}
}