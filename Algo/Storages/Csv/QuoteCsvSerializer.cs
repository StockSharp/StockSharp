#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Csv.Algo
File: QuoteCsvSerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Text;

	using Ecng.Common;

	using StockSharp.Messages;

	class NullableTimeQuoteChange
	{
		public NullableTimeQuoteChange()
		{
		}

		public NullableTimeQuoteChange(Sides side, QuoteChange quote, QuoteChangeMessage message)
		{
			if (quote == null)
				throw new ArgumentNullException(nameof(quote));

			ServerTime = message.ServerTime;
			LocalTime = message.LocalTime;
			Price = quote.Price;
			Volume = quote.Volume;
			Side = side;
			OrdersCount = quote.OrdersCount;
		}

		public DateTimeOffset ServerTime { get; set; }
		public DateTimeOffset LocalTime { get; set; }
		public decimal? Price { get; set; }
		public decimal Volume { get; set; }
		public Sides Side { get; set; }
		public int? OrdersCount { get; set; }
	}

	/// <summary>
	/// The quote serializer in the CSV format.
	/// </summary>
	class QuoteCsvSerializer : CsvMarketDataSerializer<NullableTimeQuoteChange>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QuoteCsvSerializer"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="encoding">Encoding.</param>
		public QuoteCsvSerializer(SecurityId securityId, Encoding encoding = null)
			: base(securityId, encoding)
		{
		}

		/// <inheritdoc />
		protected override void Write(CsvFileWriter writer, NullableTimeQuoteChange data, IMarketDataMetaInfo metaInfo)
		{
			writer.WriteRow(new[]
			{
				data.ServerTime.WriteTimeMls(),
				data.ServerTime.ToString("zzz"),
				data.Price?.ToString(),
				data.Volume.ToString(),
				data.Side.ToString(),
				data.OrdersCount?.ToString(),
			});

			metaInfo.LastTime = data.ServerTime.UtcDateTime;
		}

		/// <inheritdoc />
		protected override NullableTimeQuoteChange Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
		{
			var quote = new NullableTimeQuoteChange
			{
				ServerTime = reader.ReadTime(metaInfo.Date),
				Price = reader.ReadNullableDecimal(),
				Volume = reader.ReadDecimal(),
				Side = reader.ReadEnum<Sides>()
			};

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				quote.OrdersCount = reader.ReadNullableInt();

			return quote;
		}
	}
}