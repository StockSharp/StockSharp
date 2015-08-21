namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;

	using SmartFormat;
	using SmartFormat.Core.Formatting;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Ёкспорт в текстовый файл.
	/// </summary>
	public class TextExporter : BaseExporter
	{
		private readonly string _template;
		private readonly string _header;

		/// <summary>
		/// —оздать <see cref="TextExporter"/>.
		/// </summary>
		/// <param name="security">»нструмент.</param>
		/// <param name="arg">ѕараметр данных.</param>
		/// <param name="isCancelled">ќбработчик, возвращающий признак прерывани€ экспорта.</param>
		/// <param name="fileName">ѕуть к файлу.</param>
		/// <param name="template">Ўаблон форматирование строки.</param>
		/// <param name="header">«аголовок, идущий первой строкой. ≈сли передаетс€ пуста€ строка, то заголовок не будет добавлен в файл.</param>
		public TextExporter(Security security, object arg, Func<int, bool> isCancelled, string fileName, string template, string header)
			: base(security, arg, isCancelled, fileName)
		{
			if (template.IsEmpty())
				throw new ArgumentNullException("template");

			_template = template;
			_header = header;
		}

		/// <summary>
		/// Ёкспортировать <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			Do(messages);
		}

		/// <summary>
		/// Ёкспортировать <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(messages.SelectMany(d => d.Asks.Concat(d.Bids).OrderByDescending(q => q.Price).Select(q => new TimeQuoteChange(q, d))));
		}

		/// <summary>
		/// Ёкспортировать <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(messages);
		}

		/// <summary>
		/// Ёкспортировать <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			Do(messages);
		}

		/// <summary>
		/// Ёкспортировать <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(messages);
		}

		/// <summary>
		/// Ёкспортировать <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(messages);
		}

		private void Do<TValue>(IEnumerable<TValue> values)
		{
			using (var writer = new StreamWriter(Path))
			{
				if (!_header.IsEmpty())
					writer.WriteLine(_header);

				FormatCache templateCache = null;
				var formater = Smart.Default;

				foreach (var value in values)
				{
					if (!CanProcess())
						break;

					writer.WriteLine(formater.FormatWithCache(ref templateCache, _template, value));
				}

				writer.Flush();
			}
		}
	}
}