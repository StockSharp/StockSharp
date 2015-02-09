namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.IO;
	using System.Linq;

	using SmartFormat;
	using SmartFormat.Core;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Ёкспорт в текстовый файл.
	/// </summary>
	public class TextExporter : BaseExporter
	{
		/// <summary>
		/// —оздать <see cref="TextExporter"/>.
		/// </summary>
		/// <param name="security">»нструмент.</param>
		/// <param name="arg">ѕараметр данных.</param>
		/// <param name="isCancelled">ќбработчик, возвращающий признак прерывани€ экспорта.</param>
		/// <param name="fileName">ѕуть к файлу.</param>
		public TextExporter(Security security, object arg, Func<int, bool> isCancelled, string fileName)
			: base(security, arg, isCancelled, fileName)
		{
		}

		/// <summary>
		/// Ёкспортировать <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			string template;

			switch ((ExecutionTypes)Arg)
			{
				case ExecutionTypes.Tick:
				case ExecutionTypes.Trade:
					template = "txt_export_trades";
					break;
				case ExecutionTypes.Order:
					template = "txt_export_executions";
					break;
				case ExecutionTypes.OrderLog:
					template = "txt_export_orderlog";
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			Do(messages, template);
		}

		/// <summary>
		/// Ёкспортировать <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(messages.SelectMany(d => d.Asks.Concat(d.Bids).OrderByDescending(q => q.Price).Select(q => new TimeQuoteChange(q, d))), "txt_export_depths");
		}

		/// <summary>
		/// Ёкспортировать <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(messages, "txt_export_level1");
		}

		/// <summary>
		/// Ёкспортировать <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			Do(messages, "txt_export_candles");
		}

		/// <summary>
		/// Ёкспортировать <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(messages, "txt_export_news");
		}

		/// <summary>
		/// Ёкспортировать <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">—ообщени€.</param>
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(messages, "txt_export_securities");
		}

		private void Do<TValue>(IEnumerable<TValue> values, string templateName)
		{
			using (var writer = new StreamWriter(Path))
			{
				var template = ConfigurationManager.AppSettings.Get(templateName);

				FormatCache templateCache = null;
				var formater = Smart.Default;

				foreach (var value in values)
				{
					if (!CanProcess())
						break;

					writer.WriteLine(formater.FormatWithCache(ref templateCache, template, value));
				}

				writer.Flush();
			}
		}
	}
}