namespace StockSharp.Algo.Storages.Binary
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	class PositionMetaInfo : BinaryMetaInfo
	{
		public PositionMetaInfo(DateTime date)
			: base(date)
		{
			BeginValue = new RefPair<decimal, decimal>();
			CurrentValue = new RefPair<decimal, decimal>();
			BlockedValue = new RefPair<decimal, decimal>();
			CurrentPrice = new RefPair<decimal, decimal>();
			AveragePrice = new RefPair<decimal, decimal>();
			UnrealizedPnL = new RefPair<decimal, decimal>();
			RealizedPnL = new RefPair<decimal, decimal>();
			VariationMargin = new RefPair<decimal, decimal>();
			Leverage = new RefPair<decimal, decimal>();
			Commission = new RefPair<decimal, decimal>();
			CurrentValueInLots = new RefPair<decimal, decimal>();
			
			Portfolios = new List<string>();
			ClientCodes = new List<string>();
			DepoNames = new List<string>();
		}

		public RefPair<decimal, decimal> BeginValue { get; private set; }
		public RefPair<decimal, decimal> CurrentValue { get; private set; }
		public RefPair<decimal, decimal> BlockedValue { get; private set; }
		public RefPair<decimal, decimal> CurrentPrice { get; private set; }
		public RefPair<decimal, decimal> AveragePrice { get; private set; }
		public RefPair<decimal, decimal> UnrealizedPnL { get; private set; }
		public RefPair<decimal, decimal> RealizedPnL { get; private set; }
		public RefPair<decimal, decimal> VariationMargin { get; private set; }
		public RefPair<decimal, decimal> Leverage { get; private set; }
		public RefPair<decimal, decimal> Commission { get; private set; }
		public RefPair<decimal, decimal> CurrentValueInLots { get; private set; }
		
		public IList<string> Portfolios { get; }
		public IList<string> ClientCodes { get; }
		public IList<string> DepoNames { get; }

		public DateTime FirstFieldTime { get; set; }
		public DateTime LastFieldTime { get; set; }

		public override void Write(Stream stream)
		{
			base.Write(stream);

			Write(stream, BeginValue);
			Write(stream, CurrentValue);
			Write(stream, BlockedValue);
			Write(stream, CurrentPrice);
			Write(stream, AveragePrice);
			Write(stream, UnrealizedPnL);
			Write(stream, RealizedPnL);
			Write(stream, VariationMargin);
			Write(stream, Leverage);
			Write(stream, Commission);
			Write(stream, CurrentValueInLots);
			
			stream.Write(Portfolios.Count);

			foreach (var portfolio in Portfolios)
				stream.Write(portfolio);

			stream.Write(ClientCodes.Count);

			foreach (var clientCode in ClientCodes)
				stream.Write(clientCode);

			stream.Write(DepoNames.Count);

			foreach (var depoName in DepoNames)
				stream.Write(depoName);
		}

		public override void Read(Stream stream)
		{
			base.Read(stream);

			BeginValue = ReadInfo(stream);
			CurrentValue = ReadInfo(stream);
			BlockedValue = ReadInfo(stream);
			CurrentPrice = ReadInfo(stream);
			AveragePrice = ReadInfo(stream);
			UnrealizedPnL = ReadInfo(stream);
			RealizedPnL = ReadInfo(stream);
			VariationMargin = ReadInfo(stream);
			Leverage = ReadInfo(stream);
			Commission = ReadInfo(stream);
			CurrentValueInLots = ReadInfo(stream);
			
			var pfCount = stream.Read<int>();

			for (var i = 0; i < pfCount; i++)
				Portfolios.Add(stream.Read<string>());

			var ccCount = stream.Read<int>();

			for (var i = 0; i < ccCount; i++)
				ClientCodes.Add(stream.Read<string>());

			var dnCount = stream.Read<int>();

			for (var i = 0; i < dnCount; i++)
				DepoNames.Add(stream.Read<string>());
		}

		private static void Write(Stream stream, RefPair<decimal, decimal> info)
		{
			stream.Write(info.First);
			stream.Write(info.Second);
		}

		private static RefPair<decimal, decimal> ReadInfo(Stream stream)
		{
			return RefTuple.Create(stream.Read<decimal>(), stream.Read<decimal>());
		}

		public override void CopyFrom(BinaryMetaInfo src)
		{
			base.CopyFrom(src);

			var posInfo = (PositionMetaInfo)src;

			BeginValue = Clone(posInfo.BeginValue);
			CurrentValue = Clone(posInfo.CurrentValue);
			BlockedValue = Clone(posInfo.BlockedValue);
			CurrentPrice = Clone(posInfo.CurrentPrice);
			AveragePrice = Clone(posInfo.AveragePrice);
			UnrealizedPnL = Clone(posInfo.UnrealizedPnL);
			RealizedPnL = Clone(posInfo.RealizedPnL);
			VariationMargin = Clone(posInfo.VariationMargin);
			Leverage = Clone(posInfo.Leverage);
			Commission = Clone(posInfo.Commission);
			CurrentValueInLots = Clone(posInfo.CurrentValueInLots);
			
			Portfolios.Clear();
			Portfolios.AddRange(posInfo.Portfolios);

			ClientCodes.Clear();
			ClientCodes.AddRange(posInfo.ClientCodes);

			DepoNames.Clear();
			DepoNames.AddRange(posInfo.DepoNames);
		}

		private static RefPair<decimal, decimal> Clone(RefPair<decimal, decimal> info)
		{
			return RefTuple.Create(info.First, info.Second);
		}
	}

	class PositionBinarySerializer : BinaryMarketDataSerializer<PositionChangeMessage, PositionMetaInfo>
	{
		public PositionBinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider)
			: base(securityId, 20, MarketDataVersions.Version31, exchangeInfoProvider)
		{
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<PositionChangeMessage> messages, PositionMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var msg = messages.First();

				metaInfo.ServerOffset = msg.ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			foreach (var message in messages)
			{
				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(message.ServerTime, metaInfo.LastTime, "level1", true, true, metaInfo.ServerOffset, true, true, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;

				var hasLocalTime = message.HasLocalTime(message.ServerTime);

				writer.Write(hasLocalTime);

				if (hasLocalTime)
				{
					lastOffset = metaInfo.LastLocalOffset;
					metaInfo.LastLocalTime = writer.WriteTime(message.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.Str919, true, true, metaInfo.LocalOffset, true, true, ref lastOffset, true);
					metaInfo.LastLocalOffset = lastOffset;
				}

				metaInfo.Portfolios.TryAdd(message.PortfolioName);
				writer.WriteInt(metaInfo.Portfolios.IndexOf(message.PortfolioName));

				if (message.ClientCode.IsEmpty())
					writer.Write(false);
				else
				{
					writer.Write(true);

					metaInfo.ClientCodes.TryAdd(message.ClientCode);
					writer.WriteInt(metaInfo.ClientCodes.IndexOf(message.ClientCode));	
				}

				if (message.DepoName.IsEmpty())
					writer.Write(false);
				else
				{
					writer.Write(true);

					metaInfo.DepoNames.TryAdd(message.DepoName);
					writer.WriteInt(metaInfo.DepoNames.IndexOf(message.DepoName));	
				}

				writer.Write(message.LimitType != null);

				if (message.LimitType != null)
					writer.WriteInt((int)message.LimitType.Value);

				var count = message.Changes.Count;

				if (count == 0)
					throw new ArgumentException(LocalizedStrings.Str920, nameof(messages));

				writer.WriteInt(count);

				foreach (var change in message.Changes)
				{
					writer.WriteInt((int)change.Key);

					switch (change.Key)
					{
						case PositionChangeTypes.BeginValue:
							SerializeChange(writer, metaInfo.BeginValue, (decimal)change.Value);
							break;
						case PositionChangeTypes.CurrentValue:
							SerializeChange(writer, metaInfo.CurrentValue, (decimal)change.Value);
							break;
						case PositionChangeTypes.BlockedValue:
							SerializeChange(writer, metaInfo.BlockedValue, (decimal)change.Value);
							break;
						case PositionChangeTypes.CurrentPrice:
							SerializeChange(writer, metaInfo.CurrentPrice, (decimal)change.Value);
							break;
						case PositionChangeTypes.AveragePrice:
							SerializeChange(writer, metaInfo.AveragePrice, (decimal)change.Value);
							break;
						case PositionChangeTypes.UnrealizedPnL:
							SerializeChange(writer, metaInfo.UnrealizedPnL, (decimal)change.Value);
							break;
						case PositionChangeTypes.RealizedPnL:
							SerializeChange(writer, metaInfo.RealizedPnL, (decimal)change.Value);
							break;
						case PositionChangeTypes.VariationMargin:
							SerializeChange(writer, metaInfo.VariationMargin, (decimal)change.Value);
							break;
						case PositionChangeTypes.Currency:
							writer.WriteInt((int)(CurrencyTypes)change.Value);
							break;
						case PositionChangeTypes.Leverage:
							SerializeChange(writer, metaInfo.Leverage, (decimal)change.Value);
							break;
						case PositionChangeTypes.Commission:
							SerializeChange(writer, metaInfo.Commission, (decimal)change.Value);
							break;
						case PositionChangeTypes.CurrentValueInLots:
							SerializeChange(writer, metaInfo.CurrentValueInLots, (decimal)change.Value);
							break;
						case PositionChangeTypes.State:
							writer.WriteInt((int)(PortfolioStates)change.Value);
							break;
						case PositionChangeTypes.ExpirationDate:
							writer.WriteDto((DateTimeOffset)change.Value);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}

		public override PositionChangeMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			var posMsg = new PositionChangeMessage { SecurityId = SecurityId };

			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			posMsg.ServerTime = reader.ReadTime(ref prevTime, true, true, metaInfo.GetTimeZone(true, SecurityId, ExchangeInfoProvider), true, true, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			if (reader.Read())
			{
				prevTime = metaInfo.FirstLocalTime;
				lastOffset = metaInfo.FirstLocalOffset;
				posMsg.LocalTime = reader.ReadTime(ref prevTime, true, true, metaInfo.LocalOffset, true, true, ref lastOffset);
				metaInfo.FirstLocalTime = prevTime;
				metaInfo.FirstLocalOffset = lastOffset;
			}

			posMsg.PortfolioName = metaInfo.Portfolios[reader.ReadInt()];

			if (reader.Read())
			{
				posMsg.ClientCode = metaInfo.ClientCodes[reader.ReadInt()];
			}

			if (reader.Read())
			{
				posMsg.DepoName = metaInfo.DepoNames[reader.ReadInt()];
			}

			if (reader.Read())
				posMsg.LimitType = (TPlusLimits)reader.ReadInt();

			var changeCount = reader.ReadInt();

			for (var i = 0; i < changeCount; i++)
			{
				var type = (PositionChangeTypes)reader.ReadInt();

				switch (type)
				{
					case PositionChangeTypes.BeginValue:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.BeginValue));
						break;
					case PositionChangeTypes.CurrentValue:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.CurrentValue));
						break;
					case PositionChangeTypes.BlockedValue:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.BlockedValue));
						break;
					case PositionChangeTypes.CurrentPrice:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.CurrentPrice));
						break;
					case PositionChangeTypes.AveragePrice:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.AveragePrice));
						break;
					case PositionChangeTypes.UnrealizedPnL:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.UnrealizedPnL));
						break;
					case PositionChangeTypes.RealizedPnL:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.RealizedPnL));
						break;
					case PositionChangeTypes.VariationMargin:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.VariationMargin));
						break;
					case PositionChangeTypes.Currency:
						posMsg.Add(type, (CurrencyTypes)reader.ReadInt());
						break;
					case PositionChangeTypes.Leverage:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.Leverage));
						break;
					case PositionChangeTypes.Commission:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.Commission));
						break;
					case PositionChangeTypes.CurrentValueInLots:
						posMsg.Add(type, DeserializeChange(reader, metaInfo.CurrentValueInLots));
						break;
					case PositionChangeTypes.State:
						posMsg.Add(type, (PortfolioStates)reader.ReadInt());
						break;
					case PositionChangeTypes.ExpirationDate:
						posMsg.Add(type, reader.ReadDto().Value);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return posMsg;
		}

		private static void SerializeChange(BitArrayWriter writer, RefPair<decimal, decimal> info, decimal price)
		{
			//if (price == 0)
			//	throw new ArgumentOutOfRangeException(nameof(price));

			if (info.First == 0)
				info.First = info.Second = price;

			info.Second = writer.WriteDecimal(price, info.Second);
		}

		private static decimal DeserializeChange(BitArrayReader reader, RefPair<decimal, decimal> info)
		{
			info.First = reader.ReadDecimal(info.First);
			return info.First;
		}
	}
}