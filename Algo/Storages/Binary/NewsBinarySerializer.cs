#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Binary.Algo
File: NewsBinarySerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Binary
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

	class NewsMetaInfo : BinaryMetaInfo
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

	class NewsBinarySerializer : BinaryMarketDataSerializer<NewsMessage, NewsMetaInfo>
	{
		public NewsBinarySerializer(IExchangeInfoProvider exchangeInfoProvider)
			: base(default(SecurityId), 200, MarketDataVersions.Version47, exchangeInfoProvider)
		{
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<NewsMessage> messages, NewsMetaInfo metaInfo)
		{
			var isMetaEmpty = metaInfo.IsEmpty();

			writer.WriteInt(messages.Count());

			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version46;
			var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version47;

			foreach (var news in messages)
			{
				if (isMetaEmpty)
				{
					metaInfo.ServerOffset = news.ServerTime.Offset;
					isMetaEmpty = false;
				}

				writer.WriteStringEx(news.Id);

				writer.WriteString(news.Headline);

				writer.WriteStringEx(news.Story);
				writer.WriteStringEx(news.Source);
				writer.WriteStringEx(news.BoardCode);
				writer.WriteStringEx(news.SecurityId?.SecurityCode);
				writer.WriteStringEx(news.Url.To<string>());

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(news.ServerTime, metaInfo.LastTime, LocalizedStrings.News, true, true, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;
			}
		}

		public override NewsMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			var message = new NewsMessage
			{
				Id = reader.ReadStringEx(),
				Headline = reader.ReadString(),
				Story = reader.ReadStringEx(),
				Source = reader.ReadStringEx(),
				BoardCode = reader.ReadStringEx(),
				SecurityId = reader.Read() ? new SecurityId { SecurityCode = reader.ReadString() } : (SecurityId?)null,
				Url = reader.ReadStringEx().To<Uri>(),
			};

			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version46;
			var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version47;

			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			message.ServerTime = reader.ReadTime(ref prevTime, true, true, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			return message;
		}
	}
}