#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Binary.Algo
File: BinaryMarketDataSerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Binary
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	static class MarketDataVersions
	{
		//public static readonly Version Version30 = new Version(3, 0);
		public static readonly Version Version31 = new Version(3, 1);
		public static readonly Version Version33 = new Version(3, 3);
		public static readonly Version Version34 = new Version(3, 4);
		public static readonly Version Version35 = new Version(3, 5);
		public static readonly Version Version36 = new Version(3, 6);
		public static readonly Version Version40 = new Version(4, 0);
		public static readonly Version Version41 = new Version(4, 1);
		public static readonly Version Version42 = new Version(4, 2);
		public static readonly Version Version43 = new Version(4, 3);
		public static readonly Version Version44 = new Version(4, 4);
		public static readonly Version Version45 = new Version(4, 5);
		public static readonly Version Version46 = new Version(4, 6);
		public static readonly Version Version47 = new Version(4, 7);
		public static readonly Version Version48 = new Version(4, 8);
		public static readonly Version Version49 = new Version(4, 9);
		public static readonly Version Version50 = new Version(5, 0);
		public static readonly Version Version51 = new Version(5, 1);
		public static readonly Version Version52 = new Version(5, 2);
		public static readonly Version Version53 = new Version(5, 3);
		public static readonly Version Version54 = new Version(5, 4);
		public static readonly Version Version55 = new Version(5, 5);
		public static readonly Version Version56 = new Version(5, 6);
		public static readonly Version Version57 = new Version(5, 7);
		public static readonly Version Version58 = new Version(5, 8);
		public static readonly Version Version59 = new Version(5, 9);
		public static readonly Version Version60 = new Version(6, 0);
		public static readonly Version Version61 = new Version(6, 1);
		public static readonly Version Version62 = new Version(6, 2);
		public static readonly Version Version63 = new Version(6, 3);
		public static readonly Version Version64 = new Version(6, 4);
		public static readonly Version Version65 = new Version(6, 5);
		public static readonly Version Version66 = new Version(6, 6);
		public static readonly Version Version67 = new Version(6, 7);
		public static readonly Version Version68 = new Version(6, 8);
		public static readonly Version Version69 = new Version(6, 9);
	}

	abstract class BinaryMetaInfo : MetaInfo
	{
		protected BinaryMetaInfo(DateTime date)
			: base(date)
		{
			LocalOffset = DateTimeOffset.Now.Offset;

			FirstLocalTime = LastLocalTime = DateTime.UtcNow;
		}

		public Version Version { get; set; }

		public TimeSpan LocalOffset { get; private set; }
		public TimeSpan ServerOffset { get; set; }

		// сериализация и десериализация их полей сделана в дочерних классах
		public decimal FirstPrice { get; set; }
		public decimal LastPrice { get; set; }
		public decimal FirstFractionalPrice { get; set; }
		public decimal LastFractionalPrice { get; set; }
		
		public decimal FirstFractionalVolume { get; set; }
		public decimal LastFractionalVolume { get; set; }

		public DateTime FirstLocalTime { get; set; }
		public DateTime LastLocalTime { get; set; }

		public TimeSpan FirstLocalOffset { get; set; }
		public TimeSpan LastLocalOffset { get; set; }

		public TimeSpan FirstServerOffset { get; set; }
		public TimeSpan LastServerOffset { get; set; }

		public TimeSpan FirstItemLocalOffset { get; set; }
		public TimeSpan LastItemLocalOffset { get; set; }
		public DateTime FirstItemLocalTime { get; set; }
		public DateTime LastItemLocalTime { get; set; }

		public long FirstSeqNum { get; set; }
		public long PrevSeqNum { get; set; }

		public override object LastId
		{
			get => LastTime;
			set { }
		}

		public bool IsEmpty()
		{
			return Count == 0;
		}

		public override void Write(Stream stream)
		{
			stream.WriteByte((byte)Version.Major);
			stream.WriteByte((byte)Version.Minor);
			stream.WriteEx(Count);
			stream.WriteEx(PriceStep);

			if (Version < MarketDataVersions.Version40)
				stream.WriteEx(0m); // ранее был StepPrice

			stream.WriteEx(FirstTime);
			stream.WriteEx(LastTime);

			if (Version < MarketDataVersions.Version40)
				return;

			stream.WriteEx(LocalOffset);

			// размер под дополнительную информацию.
			// пока этой информации нет.
			stream.WriteEx((short)0);
		}

		public override void Read(Stream stream)
		{
			Version = new Version(stream.ReadByte(), stream.ReadByte());
			Count = stream.Read<int>();
			PriceStep = stream.Read<decimal>();

			/*FirstPriceStep = */LastPriceStep = PriceStep;

			if (Version < MarketDataVersions.Version40)
				stream.Read<decimal>(); // ранее был StepPrice

			FirstTime = stream.Read<DateTime>().UtcKind();
			LastTime = stream.Read<DateTime>().UtcKind();

			if (Version < MarketDataVersions.Version40)
				return;

			LocalOffset = stream.Read<TimeSpan>();

			// пропускаем блок данных, выделенных под дополнительную информацию
			var extInfoSize = stream.Read<short>();

			// здесь можно будет читать доп информацию из потока

			stream.Position += extInfoSize;
		}

		protected void WriteFractionalPrice(Stream stream)
		{
			if (Version < MarketDataVersions.Version43)
				return;

			stream.WriteEx(FirstFractionalPrice);
			stream.WriteEx(LastFractionalPrice);
		}

		protected void ReadFractionalPrice(Stream stream)
		{
			if (Version < MarketDataVersions.Version43)
				return;

			FirstFractionalPrice = stream.Read<decimal>();
			LastFractionalPrice = stream.Read<decimal>();
		}

		protected void WritePriceStep(Stream stream)
		{
			WriteFractionalPrice(stream);

			stream.WriteEx(/*FirstPriceStep*/0m);
			stream.WriteEx(LastPriceStep);
		}

		protected void ReadPriceStep(Stream stream)
		{
			ReadFractionalPrice(stream);

			/*FirstPriceStep = */stream.Read<decimal>();
			LastPriceStep = stream.Read<decimal>();
		}

		protected void WriteFractionalVolume(Stream stream)
		{
			if (Version < MarketDataVersions.Version44)
				return;

			stream.WriteEx(VolumeStep);
			stream.WriteEx(FirstFractionalVolume);
			stream.WriteEx(LastFractionalVolume);
		}

		protected void ReadFractionalVolume(Stream stream)
		{
			if (Version < MarketDataVersions.Version44)
				return;

			VolumeStep = stream.Read<decimal>();
			FirstFractionalVolume = stream.Read<decimal>();
			LastFractionalVolume = stream.Read<decimal>();
		}

		protected void WriteLocalTime(Stream stream, Version minVersion)
		{
			if (Version < minVersion)
				return;

			stream.WriteEx(FirstLocalTime);
			stream.WriteEx(LastLocalTime);
		}

		protected void ReadLocalTime(Stream stream, Version minVersion)
		{
			if (Version < minVersion)
				return;

			FirstLocalTime = stream.Read<DateTime>();
			LastLocalTime = stream.Read<DateTime>();
		}

		protected void WriteOffsets(Stream stream)
		{
			stream.WriteEx(FirstLocalOffset);
			stream.WriteEx(LastLocalOffset);

			stream.WriteEx(FirstServerOffset);
			stream.WriteEx(LastServerOffset);
		}

		protected void ReadOffsets(Stream stream)
		{
			FirstLocalOffset = stream.Read<TimeSpan>();
			LastLocalOffset = stream.Read<TimeSpan>();

			FirstServerOffset = stream.Read<TimeSpan>();
			LastServerOffset = stream.Read<TimeSpan>();
		}

		protected void WriteSeqNums(Stream stream)
		{
			stream.WriteEx(FirstSeqNum);
			stream.WriteEx(PrevSeqNum);
		}

		protected void ReadSeqNums(Stream stream)
		{
			FirstSeqNum = stream.Read<long>();
			PrevSeqNum = stream.Read<long>();
		}

		protected void WriteItemLocalOffset(Stream stream, Version minVersion)
		{
			if (Version < minVersion)
				return;

			stream.WriteEx(FirstItemLocalOffset);
			stream.WriteEx(LastItemLocalOffset);
		}

		protected void ReadItemLocalOffset(Stream stream, Version minVersion)
		{
			if (Version < minVersion)
				return;

			FirstItemLocalOffset = stream.Read<TimeSpan>();
			LastItemLocalOffset = stream.Read<TimeSpan>();
		}

		protected void WriteItemLocalTime(Stream stream, Version minVersion)
		{
			if (Version < minVersion)
				return;

			stream.WriteEx(FirstItemLocalTime);
			stream.WriteEx(LastItemLocalTime);
		}

		protected void ReadItemLocalTime(Stream stream, Version minVersion)
		{
			if (Version < minVersion)
				return;

			FirstItemLocalTime = stream.Read<DateTime>();
			LastItemLocalTime = stream.Read<DateTime>();
		}

		//public override TMetaInfo Clone()
		//{
		//	var copy = typeof(TMetaInfo).CreateInstance<TMetaInfo>(Date);
		//	copy.CopyFrom((TMetaInfo)this);
		//	return copy;
		//}

		public virtual void CopyFrom(BinaryMetaInfo src)
		{
			Version = src.Version;
			Count = src.Count;
			PriceStep = src.PriceStep;
			//StepPrice = src.StepPrice;
			FirstTime = src.FirstTime;
			LastTime = src.LastTime;
			LocalOffset = src.LocalOffset;
			ServerOffset = src.ServerOffset;
			FirstFractionalPrice = src.FirstFractionalPrice;
			LastFractionalPrice = src.LastFractionalPrice;
			VolumeStep = src.VolumeStep;
			FirstFractionalVolume = src.FirstFractionalVolume;
			LastFractionalVolume = src.LastFractionalVolume;
			FirstLocalTime = src.FirstLocalTime;
			LastLocalTime = src.LastLocalTime;
			FirstLocalOffset = src.FirstLocalOffset;
			LastLocalOffset = src.LastLocalOffset;
			FirstServerOffset = src.FirstServerOffset;
			LastServerOffset = src.LastServerOffset;
			FirstItemLocalTime = src.FirstItemLocalTime;
			LastItemLocalTime = src.LastItemLocalTime;
			FirstItemLocalOffset = src.FirstItemLocalOffset;
			LastItemLocalOffset = src.LastItemLocalOffset;
			//FirstPriceStep = src.FirstPriceStep;
			LastPriceStep = src.LastPriceStep;
			FirstPrice = src.FirstPrice;
			LastPrice = src.LastPrice;

			FirstSeqNum = src.FirstSeqNum;
			PrevSeqNum = src.PrevSeqNum;
		}
	}

	abstract class BinaryMarketDataSerializer<TData, TMetaInfo> : IMarketDataSerializer<TData>
		where TMetaInfo : BinaryMetaInfo
	{
		public class MarketDataEnumerator : SimpleEnumerator<TData>
		{
			private readonly TMetaInfo _originalMetaInfo;

			public MarketDataEnumerator(BinaryMarketDataSerializer<TData, TMetaInfo> serializer, BitArrayReader reader, TMetaInfo metaInfo)
			{
				Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
				Index = -1;
				Reader = reader ?? throw new ArgumentNullException(nameof(reader));

				_originalMetaInfo = metaInfo ?? throw new ArgumentNullException(nameof(metaInfo));
			}

			public BitArrayReader Reader { get; }

			public TMetaInfo MetaInfo { get; private set; }

			public BinaryMarketDataSerializer<TData, TMetaInfo> Serializer { get; }

			public int Index { get; private set; }

			public int PartSize { get; private set; }

			public TData Previous { get; private set; }

			public TData Delta { get; internal set; }

			public override bool MoveNext()
			{
				if (Index < 0) // enumerator стоит перед первой записью
				{
					MetaInfo = (TMetaInfo)((IMarketDataSerializer)Serializer).CreateMetaInfo(_originalMetaInfo.Date);
					MetaInfo.CopyFrom(_originalMetaInfo);
					Index = 0;
				}

				if (Index >= MetaInfo.Count)
					return false;

				if (Index == PartSize)
					PartSize += Reader.ReadInt();

				Current = Serializer.MoveNext(this);
				Previous = Current;

				if (Index == (PartSize - 1))
				{
					//Reader.AlignReader();
					if ((Reader.Offset % 8) != 0)
					{
						var shift = ((Reader.Offset / 8) * 8 + 8) - Reader.Offset;
						Reader.Offset += shift;
					}
				}

				Index++;

				return true;
			}

			public override void Reset()
			{
				Index = -1;
				MetaInfo = null;
				Previous = Current = default;
				PartSize = 0;

				if (Reader != null)
					Reader.Offset = 0;
			}
		}

		protected BinaryMarketDataSerializer(SecurityId securityId, object arg, int dataSize, Version version, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (securityId == null)
				throw new ArgumentNullException(nameof(securityId));

			Arg = arg;
			SecurityId = securityId;
			DataSize = dataSize;

			Version = version;
			ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
		}

		protected object Arg { get; }
		protected SecurityId SecurityId { get; }
		protected int DataSize { get; }
		protected Version Version { get; set; }
		protected IExchangeInfoProvider ExchangeInfoProvider { get; }

		public TimeSpan TimePrecision { get; } = TimeSpan.FromTicks(1);

		public StorageFormats Format => StorageFormats.Binary;

		IMarketDataMetaInfo IMarketDataSerializer.CreateMetaInfo(DateTime date)
		{
			var info = typeof(TMetaInfo).CreateInstance<TMetaInfo>(date);
			info.Version = Version;
			return info;
		}

		void IMarketDataSerializer.Serialize(Stream stream, IEnumerable data, IMarketDataMetaInfo metaInfo)
		{
			Serialize(stream, data.Cast<TData>(), metaInfo);
		}

		IEnumerable IMarketDataSerializer.Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			return Deserialize(stream, metaInfo);
		}

		private void CheckVersion(TMetaInfo metaInfo, string operation)
		{
			if (metaInfo.Version <= Version)
				return;

			var name = $"{SecurityId}/{typeof(TData)}/{Arg}";
			Debug.WriteLine($"Storage ({operation}) !! DISABLED !!: {name}");

			throw new InvalidOperationException(LocalizedStrings.StorageVersionNewerKey.Put(name, metaInfo.Version, Version));
		}

		public void Serialize(Stream stream, IEnumerable<TData> data, IMarketDataMetaInfo metaInfo)
		{
			var typedInfo = (TMetaInfo)metaInfo;
			CheckVersion(typedInfo, "Save");
			//var temp = new MemoryStream { Capacity = DataSize * data.Count() * 2 };

			using (var writer = new BitArrayWriter(stream))
				OnSave(writer, data, typedInfo);

			//return stream.To<byte[]>();
		}

		public IEnumerable<TData> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			var typedInfo = (TMetaInfo)metaInfo;
			CheckVersion(typedInfo, "Load");

			var data = new MemoryStream();
			stream.CopyTo(data);
			stream.Dispose();

			return new SimpleEnumerable<TData>(() => new MarketDataEnumerator(this, new BitArrayReader(data), typedInfo));
		}

		protected abstract void OnSave(BitArrayWriter writer, IEnumerable<TData> data, TMetaInfo metaInfo);
		public abstract TData MoveNext(MarketDataEnumerator enumerator);

		protected void WriteItemLocalTime(BitArrayWriter writer, TMetaInfo metaInfo, Message message, bool isTickPrecision)
		{
			var lastLocalOffset = metaInfo.LastItemLocalOffset;
			metaInfo.LastItemLocalTime = writer.WriteTime(message.LocalTime, metaInfo.LastItemLocalTime, "local time", true, true, metaInfo.LocalOffset, true, isTickPrecision, ref lastLocalOffset, true);
			metaInfo.LastItemLocalOffset = lastLocalOffset;
		}

		protected DateTimeOffset ReadItemLocalTime(BitArrayReader reader, TMetaInfo metaInfo, bool isTickPrecision)
		{
			var prevTsTime = metaInfo.FirstItemLocalTime;
			var lastOffset = metaInfo.FirstItemLocalOffset;
			var retVal = reader.ReadTime(ref prevTsTime, true, true, lastOffset, true, isTickPrecision, ref lastOffset);
			metaInfo.FirstItemLocalTime = prevTsTime;
			metaInfo.FirstItemLocalOffset = lastOffset;
			return retVal;
		}
	}
}