namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Serialization;

	using StockSharp.Messages;

	static class MarketDataVersions
	{
		//public static readonly Version Version30 = new Version(3, 0);
		public static readonly Version Version31 = new Version(3, 1);
		public static readonly Version Version33 = new Version(3, 3);
		public static readonly Version Version34 = new Version(3, 4);
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
	}

	abstract class BinaryMetaInfo<TMetaInfo> : MetaInfo<TMetaInfo>
		where TMetaInfo : BinaryMetaInfo<TMetaInfo>
	{
		protected BinaryMetaInfo(DateTime date)
			: base(date)
		{
			LocalOffset = TimeHelper.TimeZoneOffset;

			FirstLocalTime = date;
			LastLocalTime = date;
		}

		public Version Version { get; set; }

		public TimeSpan LocalOffset { get; private set; }
		public TimeSpan ServerOffset { get; set; }

		// сериализация и десериализация их полей сделана в дочерних классах
		public decimal FirstPrice { get; set; }
		public decimal LastPrice { get; set; }
		public decimal FirstNonSystemPrice { get; set; }
		public decimal LastNonSystemPrice { get; set; }

		public decimal FirstFractionalVolume { get; set; }
		public decimal LastFractionalVolume { get; set; }

		public DateTime FirstLocalTime { get; set; }
		public DateTime LastLocalTime { get; set; }

		public bool IsEmpty()
		{
			return Count == 0;
		}

		public override void Write(Stream stream)
		{
			stream.WriteByte((byte)Version.Major);
			stream.WriteByte((byte)Version.Minor);
			stream.Write(Count);
			stream.Write(PriceStep);

			if (Version < MarketDataVersions.Version40)
				stream.Write(0m); // ранее был StepPrice

			stream.Write(FirstTime);
			stream.Write(LastTime);

			if (Version < MarketDataVersions.Version40)
				return;

			stream.Write(LocalOffset);

			// размер под дополнительную информацию.
			// пока этой информации нет.
			stream.Write((short)0);
		}

		public override void Read(Stream stream)
		{
			Version = new Version(stream.ReadByte(), stream.ReadByte());
			Count = stream.Read<int>();
			PriceStep = stream.Read<decimal>();

			if (Version < MarketDataVersions.Version40)
				stream.Read<decimal>(); // ранее был StepPrice

			FirstTime = stream.Read<DateTime>().ChangeKind(DateTimeKind.Utc);
			LastTime = stream.Read<DateTime>().ChangeKind(DateTimeKind.Utc);

			if (Version < MarketDataVersions.Version40)
				return;

			LocalOffset = stream.Read<TimeSpan>();

			// пропускаем блок данных, выделенных под дополнительную информацию
			var extInfoSize = stream.Read<short>();

			// здесь можно будет читать доп информацию из потока

			stream.Position += extInfoSize;
		}

		protected void WriteNonSystemPrice(Stream stream)
		{
			if (Version < MarketDataVersions.Version43)
				return;

			stream.Write(FirstNonSystemPrice);
			stream.Write(LastNonSystemPrice);
		}

		protected void ReadNonSystemPrice(Stream stream)
		{
			if (Version < MarketDataVersions.Version43)
				return;

			FirstNonSystemPrice = stream.Read<decimal>();
			LastNonSystemPrice = stream.Read<decimal>();
		}

		protected void WriteFractionalVolume(Stream stream)
		{
			if (Version < MarketDataVersions.Version44)
				return;

			stream.Write(VolumeStep);
			stream.Write(FirstFractionalVolume);
			stream.Write(LastFractionalVolume);
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

			stream.Write(FirstLocalTime);
			stream.Write(LastLocalTime);
		}

		protected void ReadLocalTime(Stream stream, Version minVersion)
		{
			if (Version < minVersion)
				return;

			FirstLocalTime = stream.Read<DateTime>();
			LastLocalTime = stream.Read<DateTime>();
		}

		public override TMetaInfo Clone()
		{
			var copy = typeof(TMetaInfo).CreateInstance<TMetaInfo>(Date);
			copy.CopyFrom((TMetaInfo)this);
			return copy;
		}

		protected virtual void CopyFrom(TMetaInfo src)
		{
			Version = src.Version;
			Count = src.Count;
			PriceStep = src.PriceStep;
			//StepPrice = src.StepPrice;
			FirstTime = src.FirstTime;
			LastTime = src.LastTime;
			LocalOffset = src.LocalOffset;
			ServerOffset = src.ServerOffset;
			FirstNonSystemPrice = src.FirstNonSystemPrice;
			LastNonSystemPrice = src.LastNonSystemPrice;
			VolumeStep = src.VolumeStep;
			FirstFractionalVolume = src.FirstFractionalVolume;
			LastFractionalVolume = src.LastFractionalVolume;
			FirstLocalTime = src.FirstLocalTime;
			LastLocalTime = src.LastLocalTime;
		}
	}

	abstract class BinaryMarketDataSerializer<TData, TMetaInfo> : IMarketDataSerializer<TData>
		where TMetaInfo : BinaryMetaInfo<TMetaInfo>
	{
		public class MarketDataEnumerator : SimpleEnumerator<TData>
		{
			private readonly TMetaInfo _originalMetaInfo;

			public MarketDataEnumerator(BinaryMarketDataSerializer<TData, TMetaInfo> serializer, BitArrayReader reader, TMetaInfo metaInfo)
			{
				if (serializer == null)
					throw new ArgumentNullException("serializer");

				if (reader == null)
					throw new ArgumentNullException("reader");

				if (metaInfo == null)
					throw new ArgumentNullException("metaInfo");

				Serializer = serializer;
				Index = -1;
				Reader = reader;

				_originalMetaInfo = metaInfo;
			}

			public BitArrayReader Reader { get; private set; }

			public TMetaInfo MetaInfo { get; private set; }

			public BinaryMarketDataSerializer<TData, TMetaInfo> Serializer { get; private set; }

			public int Index { get; private set; }

			public int PartSize { get; private set; }

			public TData Previous { get; private set; }

			public TData Delta { get; internal set; }

			public override bool MoveNext()
			{
				if (Index < 0) // enumerator стоит перед первой записью
				{
					MetaInfo = _originalMetaInfo.Clone();
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
				Previous = Current = default(TData);
				PartSize = 0;

				if (Reader != null)
					Reader.Offset = 0;
			}
		}

		protected BinaryMarketDataSerializer(SecurityId securityId, int dataSize)
		{
			if (securityId == null)
				throw new ArgumentNullException("securityId");

			SecurityId = securityId;
			DataSize = dataSize;

			Version = MarketDataVersions.Version44;
		}

		protected SecurityId SecurityId { get; private set; }
		protected int DataSize { get; private set; }
		protected Version Version { get; set; }

		IMarketDataMetaInfo IMarketDataSerializer.CreateMetaInfo(DateTime date)
		{
			var info = MetaInfo<TMetaInfo>.CreateMetaInfo(date);
			info.Version = Version;
			return info;
		}

		byte[] IMarketDataSerializer.Serialize(IEnumerable data, IMarketDataMetaInfo metaInfo)
		{
			return Serialize(data.Cast<TData>(), metaInfo);
		}

		IEnumerableEx IMarketDataSerializer.Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			return Deserialize(stream, metaInfo);
		}

		public byte[] Serialize(IEnumerable<TData> data, IMarketDataMetaInfo metaInfo)
		{
			var writer = new BitArrayWriter(DataSize * data.Count() * 2);

			OnSave(writer, data, (TMetaInfo)metaInfo);

			return writer.GetBytes();
		}

		public IEnumerableEx<TData> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			// TODO сделать BitArrayReader работающим с потоком
			var data = stream.ReadBuffer();
			stream.Dispose();

			return new SimpleEnumerable<TData>(() => new MarketDataEnumerator(this, new BitArrayReader(data), (TMetaInfo)metaInfo))
				.ToEx(metaInfo.Count);
		}

		public IEnumerableEx<TData> Deserialize(IDataStorageReader<TData> reader)
		{
			var stream = reader.Stream;
			var metaInfo = reader.MetaInfo;
			var data = stream.ReadBuffer();

			return new SimpleEnumerable<TData>(() => new MarketDataEnumerator(this, new BitArrayReader(data), (TMetaInfo)metaInfo))
				.ToEx(metaInfo.Count);
		}

		protected abstract void OnSave(BitArrayWriter writer, IEnumerable<TData> data, TMetaInfo metaInfo);
		public abstract TData MoveNext(MarketDataEnumerator enumerator);
	}
}