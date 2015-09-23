namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	class NewsMetaInfo : BinaryMetaInfo<NewsMetaInfo>
	{
		public NewsMetaInfo(DateTime date)
			: base(date)
		{
		}

		public override void Read(Stream stream)
		{
			base.Read(stream);

			if (Version < MarketDataVersions.Version45)
				return;

			ServerOffset = stream.Read<TimeSpan>();

			if (Version < MarketDataVersions.Version46)
				return;

			ReadOffsets(stream);
		}

		public override void Write(Stream stream)
		{
			base.Write(stream);

			if (Version < MarketDataVersions.Version45)
				return;

			stream.Write(ServerOffset);

			if (Version < MarketDataVersions.Version46)
				return;

			WriteOffsets(stream);
		}
	}

	class NewsSerializer : BinaryMarketDataSerializer<NewsMessage, NewsMetaInfo>
	{
		public NewsSerializer()
			: base(default(SecurityId), 200, MarketDataVersions.Version46)
		{
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<NewsMessage> messages, NewsMetaInfo metaInfo)
		{
			var isMetaEmpty = metaInfo.IsEmpty();

			writer.WriteInt(messages.Count());

			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version46;
			
			foreach (var news in messages)
			{
				if (isMetaEmpty)
				{
					metaInfo.ServerOffset = news.ServerTime.Offset;
					isMetaEmpty = false;
				}

				if (news.Id.IsEmpty())
					writer.Write(false);
				else
				{
					writer.Write(true);
					writer.WriteString(news.Id);
				}

				writer.WriteString(news.Headline);

				if (news.Story.IsEmpty())
					writer.Write(false);
				else
				{
					writer.Write(true);
					writer.WriteString(news.Story);
				}

				if (news.Source.IsEmpty())
					writer.Write(false);
				else
				{
					writer.Write(true);
					writer.WriteString(news.Source);
				}

				if (news.BoardCode.IsEmpty())
					writer.Write(false);
				else
				{
					writer.Write(true);
					writer.WriteString(news.BoardCode);
				}

				if (news.SecurityId == null)
					writer.Write(false);
				else
				{
					writer.Write(true);
					writer.WriteString(news.SecurityId.Value.SecurityCode);
				}

				if (news.Url == null)
					writer.Write(false);
				else
				{
					writer.Write(true);
					writer.WriteString(news.Url.To<string>());
				}

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(news.ServerTime, metaInfo.LastTime, LocalizedStrings.News, true, true, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;
			}
		}

		public override NewsMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			var message = new NewsMessage
			{
				Id = reader.Read() ? reader.ReadString() : null,
				Headline = reader.ReadString(),
				Story = reader.Read() ? reader.ReadString() : null,
				Source = reader.Read() ? reader.ReadString() : null,
				BoardCode = reader.Read() ? reader.ReadString() : null,
				SecurityId = reader.Read() ? new SecurityId { SecurityCode = reader.ReadString() } : (SecurityId?)null,
				Url = reader.Read() ? reader.ReadString().To<Uri>() : null,
			};

			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version46;

			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			message.ServerTime = reader.ReadTime(ref prevTime, true, true, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			return message;
		}
	}
}