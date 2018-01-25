#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Algo
File: TextExporter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Common;

	using SmartFormat;
	using SmartFormat.Core.Formatting;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The export into text file.
	/// </summary>
	public class TextExporter : BaseExporter
	{
		private readonly string _template;
		private readonly string _header;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextExporter"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The data parameter.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <param name="fileName">The path to file.</param>
		/// <param name="template">The string formatting template.</param>
		/// <param name="header">Header at the first line. Do not add header while empty string.</param>
		public TextExporter(Security security, object arg, Func<int, bool> isCancelled, string fileName, string template, string header)
			: base(security, arg, isCancelled, fileName)
		{
			if (template.IsEmpty())
				throw new ArgumentNullException(nameof(template));

			_template = template;
			_header = header;
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			Do(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(messages.ToTimeQuotes());
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			Do(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<PositionChangeMessage> messages)
		{
			Do(messages);
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<IndicatorValue> values)
		{
			Do(values);
		}

		private void Do<TValue>(IEnumerable<TValue> values)
		{
			using (var writer = new StreamWriter(Path, true))
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

				//writer.Flush();
			}
		}
	}
}