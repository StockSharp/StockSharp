#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Csv.Algo
File: CandleCsvSerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The candle serializer in the CSV format.
	/// </summary>
	/// <typeparam name="TCandleMessage"><see cref="CandleMessage"/> derived type.</typeparam>
	public class CandleCsvSerializer<TCandleMessage> : CsvMarketDataSerializer<TCandleMessage>
		where TCandleMessage : CandleMessage, new()
	{
		private class CandleCsvMetaInfo : MetaInfo
			//where TCandleMessage : CandleMessage, new()
		{
			private readonly Dictionary<DateTime, TCandleMessage> _items = new Dictionary<DateTime, TCandleMessage>();

			private readonly CandleCsvSerializer<TCandleMessage> _serializer;
			private readonly Encoding _encoding;

			private bool _isOverride;

			public override bool IsOverride => _isOverride;

			public CandleCsvMetaInfo(CandleCsvSerializer<TCandleMessage> serializer, DateTime date, Encoding encoding)
				: base(date)
			{
				_serializer = serializer;
				_encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
			}

			public override object LastId { get; set; }

			public override void Write(Stream stream)
			{
			}

			public override void Read(Stream stream)
			{
				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					var count = 0;
					var firstTimeRead = false;

					var reader = new FastCsvReader(stream, _encoding);

					while (reader.NextLine())
					{
						var message = _serializer.Read(reader, this);

						var openTime = message.OpenTime.UtcDateTime;

						_items.Add(openTime, message);

						if (!firstTimeRead)
						{
							FirstTime = openTime;
							firstTimeRead = true;
						}

						LastTime = openTime;

						count++;
					}

					Count = count;

					stream.Position = 0;
				});
			}

			public IEnumerable<TCandleMessage> Process(IEnumerable<TCandleMessage> messages)
			{
				messages = messages.ToArray();

				if (messages.IsEmpty())
					return Enumerable.Empty<TCandleMessage>();

				foreach (var message in messages)
				{
					var openTime = message.OpenTime.UtcDateTime;

					if (!_isOverride)
						_isOverride = _items.ContainsKey(openTime) || openTime <= LastTime;

					_items[openTime] = message;

					LastTime = openTime;
				}

				return _isOverride ? _items.Values : messages;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleCsvSerializer{TCandleMessage}"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="encoding">Encoding.</param>
		public CandleCsvSerializer(SecurityId securityId, object arg, Encoding encoding = null)
			: base(securityId, encoding)
		{
			Arg = arg ?? throw new ArgumentNullException(nameof(arg));
		}

		/// <summary>
		/// Candle arg.
		/// </summary>
		public object Arg { get; set; }

		/// <summary>
		/// To create empty meta-information.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>Meta-information on data for one day.</returns>
		public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
		{
			return new CandleCsvMetaInfo(this, date, Encoding);
		}

		/// <summary>
		/// Save data into stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		/// <param name="data">Data.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		public override void Serialize(Stream stream, IEnumerable<TCandleMessage> data, IMarketDataMetaInfo metaInfo)
		{
			var candleMetaInfo = (CandleCsvMetaInfo)metaInfo;

			var toWrite = candleMetaInfo.Process(data);

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				var writer = new CsvFileWriter(stream, Encoding);

				try
				{
					foreach (var item in toWrite)
					{
						Write(writer, item, candleMetaInfo);
					}
				}
				finally
				{
					writer.Writer.Flush();
				}
			});
		}

		/// <summary>
		/// Write data to the specified writer.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Data.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		protected override void Write(CsvFileWriter writer, TCandleMessage data, IMarketDataMetaInfo metaInfo)
		{
			if (data.State == CandleStates.Active)
				throw new ArgumentException(LocalizedStrings.CandleActiveNotSupport.Put(data), nameof(data));

			writer.WriteRow(new[]
			{
				data.OpenTime.WriteTimeMls(),
				data.OpenTime.ToString("zzz"),
				data.OpenPrice.ToString(),
				data.HighPrice.ToString(),
				data.LowPrice.ToString(),
				data.ClosePrice.ToString(),
				data.TotalVolume.ToString()
			});
		}

		/// <summary>
		/// Read data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		/// <returns>Data.</returns>
		protected override TCandleMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
		{
			return new TCandleMessage
			{
				SecurityId = SecurityId,
				Arg = Arg,
				OpenTime = reader.ReadTime(metaInfo.Date),
				OpenPrice = reader.ReadDecimal(),
				HighPrice = reader.ReadDecimal(),
				LowPrice = reader.ReadDecimal(),
				ClosePrice = reader.ReadDecimal(),
				TotalVolume = reader.ReadDecimal(),
				State = CandleStates.Finished
			};
		}
	}
}