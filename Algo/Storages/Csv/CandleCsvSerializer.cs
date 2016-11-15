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
	using System.Text;

	using Ecng.Common;

	using StockSharp.Messages;

	class CandleCsvMetaInfo<TCandleMessage> : MetaInfo
		where TCandleMessage : CandleMessage, new()
	{
		private readonly Dictionary<DateTime, TCandleMessage> _items = new Dictionary<DateTime, TCandleMessage>(); 

		private readonly Encoding _encoding;
		private readonly SecurityId _securityId;
		private readonly object _arg;

		private bool _isOverride;

		public override bool IsOverride => _isOverride;

		public CandleCsvMetaInfo(DateTime date, Encoding encoding, SecurityId securityId, object arg)
			: base(date)
		{
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));

			if (arg == null)
				throw new ArgumentNullException(nameof(arg));

			_encoding = encoding;
			_securityId = securityId;
			_arg = arg;
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
					var message = Read(reader);

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

		public TCandleMessage Read(FastCsvReader reader)
		{
			return new TCandleMessage
			{
				SecurityId = _securityId,
				Arg = _arg,
				OpenTime = reader.ReadTime(Date),
				OpenPrice = reader.ReadDecimal(),
				HighPrice = reader.ReadDecimal(),
				LowPrice = reader.ReadDecimal(),
				ClosePrice = reader.ReadDecimal(),
				TotalVolume = reader.ReadDecimal(),
				State = CandleStates.Finished
			};
		}

		public void Write(CsvFileWriter writer, TCandleMessage message)
		{
			var openTime = message.OpenTime.UtcDateTime;

			if (!_items.ContainsKey(openTime) && openTime > LastTime)
			{
				_isOverride = false;
			}
			else
				_isOverride = true;

			_items[openTime] = message;

			if (_isOverride)
			{
				foreach (var data in _items.Values)
					WriteData(writer, data);
			}
			else
				WriteData(writer, message);
		}

		private void WriteData(CsvFileWriter writer, TCandleMessage data)
		{
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

			LastTime = data.OpenTime.UtcDateTime;
		}
	}

	/// <summary>
	/// The candle serializer in the CSV format.
	/// </summary>
	public class CandleCsvSerializer<TCandleMessage> : CsvMarketDataSerializer<TCandleMessage>
		where TCandleMessage : CandleMessage, new()
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CandleCsvSerializer{TCandleMessage}"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="encoding">Encoding.</param>
		public CandleCsvSerializer(SecurityId securityId, object arg, Encoding encoding = null)
			: base(securityId, encoding)
		{
			if (arg == null)
				throw new ArgumentNullException(nameof(arg));

			Arg = arg;
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
			return new CandleCsvMetaInfo<TCandleMessage>(date, Encoding, SecurityId, Arg);
		}

		/// <summary>
		/// Write data to the specified writer.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Data.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		protected override void Write(CsvFileWriter writer, TCandleMessage data, IMarketDataMetaInfo metaInfo)
		{
			((CandleCsvMetaInfo<TCandleMessage>)metaInfo).Write(writer, data);
		}

		/// <summary>
		/// Read data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		/// <returns>Data.</returns>
		protected override TCandleMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
		{
			return ((CandleCsvMetaInfo<TCandleMessage>)metaInfo).Read(reader);
		}
	}
}