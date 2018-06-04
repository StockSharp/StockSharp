#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Csv.Algo
File: CsvMarketDataSerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	class CsvMetaInfo : MetaInfo
	{
		private readonly Encoding _encoding;
		private readonly Func<FastCsvReader, object> _readId;

		public CsvMetaInfo(DateTime date, Encoding encoding, Func<FastCsvReader, object> readId)
			: base(date)
		{
			_encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
			_readId = readId;
		}

		//public override CsvMetaInfo Clone()
		//{
		//	return new CsvMetaInfo(Date, _encoding, _toId)
		//	{
		//		Count = Count,
		//		FirstTime = FirstTime,
		//		LastTime = LastTime,
		//		PriceStep = PriceStep,
		//		VolumeStep = VolumeStep,
		//	};
		//}

		private object _lastId;

		public override object LastId
		{
			get => _lastId;
			set => _lastId = value;
		}

		public override void Write(Stream stream)
		{
		}

		public override void Read(Stream stream)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				var count = 0;

				var firstTimeRead = false;
				string lastLine = null;

				var reader = new FastCsvReader(stream, _encoding);

				while (reader.NextLine())
				{
					lastLine = reader.CurrentLine;

					if (!firstTimeRead)
					{
						FirstTime = reader.ReadTime(Date).UtcDateTime;
						firstTimeRead = true;
					}

					count++;
				}

				Count = count;

				if (lastLine != null)
				{
					reader = new FastCsvReader(lastLine);

					if (!reader.NextLine())
						throw new InvalidOperationException();

					LastTime = reader.ReadTime(Date).UtcDateTime;
					_lastId = _readId?.Invoke(reader);
				}

				stream.Position = 0;
			});
		}
	}

	/// <summary>
	/// The serializer in the CSV format.
	/// </summary>
	/// <typeparam name="TData">Data type.</typeparam>
	public abstract class CsvMarketDataSerializer<TData> : IMarketDataSerializer<TData>
	{
		// ReSharper disable StaticFieldInGenericType
		private static readonly UTF8Encoding _utf = new UTF8Encoding(false);
		// ReSharper restore StaticFieldInGenericType

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvMarketDataSerializer{T}"/>.
		/// </summary>
		/// <param name="encoding">Encoding.</param>
		protected CsvMarketDataSerializer(Encoding encoding = null)
			: this(default(SecurityId), encoding)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvMarketDataSerializer{T}"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="encoding">Encoding.</param>
		protected CsvMarketDataSerializer(SecurityId securityId, Encoding encoding = null)
		{
			SecurityId = securityId;
			Encoding = encoding ?? _utf;
		}

		/// <summary>
		/// Encoding.
		/// </summary>
		public Encoding Encoding { get; }

		/// <summary>
		/// Security ID.
		/// </summary>
		public SecurityId SecurityId { get; }

		/// <summary>
		/// Storage format.
		/// </summary>
		public StorageFormats Format => StorageFormats.Csv;

		/// <summary>
		/// Time precision.
		/// </summary>
		public TimeSpan TimePrecision { get; } = TimeSpan.FromMilliseconds(1);

		/// <summary>
		/// To create empty meta-information.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>Meta-information on data for one day.</returns>
		public virtual IMarketDataMetaInfo CreateMetaInfo(DateTime date)
		{
			return new CsvMetaInfo(date, Encoding, null);
		}

		void IMarketDataSerializer.Serialize(Stream stream, IEnumerable data, IMarketDataMetaInfo metaInfo)
		{
			Serialize(stream, data.Cast<TData>(), metaInfo);
		}

		IEnumerable IMarketDataSerializer.Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			return Deserialize(stream, metaInfo);
		}

		/// <summary>
		/// Save data into stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		/// <param name="data">Data.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		public virtual void Serialize(Stream stream, IEnumerable<TData> data, IMarketDataMetaInfo metaInfo)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				var writer = new CsvFileWriter(stream, Encoding);

				try
				{
					foreach (var item in data)
					{
						Write(writer, item, metaInfo);
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
		protected abstract void Write(CsvFileWriter writer, TData data, IMarketDataMetaInfo metaInfo);

		private class CsvEnumerator : SimpleEnumerator<TData>
		{
			private readonly CsvMarketDataSerializer<TData> _serializer;
			private readonly FastCsvReader _reader;
			private readonly IMarketDataMetaInfo _metaInfo;

			public CsvEnumerator(CsvMarketDataSerializer<TData> serializer, FastCsvReader reader, IMarketDataMetaInfo metaInfo)
			{
				_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
				_reader = reader ?? throw new ArgumentNullException(nameof(reader));
				_metaInfo = metaInfo ?? throw new ArgumentNullException(nameof(metaInfo));
			}

			public override bool MoveNext()
			{
				var retVal = _reader.NextLine();

				if (retVal)
					Current = _serializer.Read(_reader, _metaInfo);

				return retVal;
			}
		}

		/// <summary>
		/// To load data from the stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		/// <returns>Data.</returns>
		public virtual IEnumerable<TData> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			// TODO (переделать в будущем)
			var copy = new MemoryStream();
			stream.CopyTo(copy);
			copy.Position = 0;

			stream.Dispose();

			//return new SimpleEnumerable<TData>(() =>
			//	new CsvReader(copy, _encoding, SecurityId, metaInfo.Date.Date, _executionType, _candleArg, _members))
			//	.ToEx(metaInfo.Count);

			return new SimpleEnumerable<TData>(() => new CsvEnumerator(this, new FastCsvReader(copy, Encoding), metaInfo));
		}

		/// <summary>
		/// Read data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		/// <returns>Data.</returns>
		protected abstract TData Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo);
	}
}