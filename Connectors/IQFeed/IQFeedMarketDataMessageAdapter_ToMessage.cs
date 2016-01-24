#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.IQFeed.IQFeed
File: IQFeedMarketDataMessageAdapter_ToMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.IQFeed
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Algo;

	/// <summary>
	/// The messages adapter for IQFeed.
	/// </summary>
	partial class IQFeedMarketDataMessageAdapter
	{
		private IEnumerable<Message> ToSecurityFundamentalMessages(string value)
		{
			var parts = value.SplitByComma();

			var pe = parts[2].To<decimal?>();
			var beta = parts[26].To<decimal?>();
			var precision = parts[39].To<int>();
			var sic = parts[40].To<int?>();
			var historicalVolatility = parts[41].To<decimal?>();
			var securityType = parts[42].To<int>();
			var listedMarket = parts[43].To<int>();
			var maturityDate = parts[49].TryToDateTime("MM/dd/yyyy").ToEst();
			var expirationDate = parts[51].TryToDateTime("MM/dd/yyyy").ToEst();
			var strikePrice = parts[52].To<decimal?>();
			var naics = parts[53].To<int?>();
			var exchangeRoot = parts[54];

			var secId = CreateSecurityId(parts[0], listedMarket);

			yield return new SecurityMessage
			{
				SecurityId = secId,
				Decimals = precision,
				Strike = strikePrice ?? 0,
				ExpiryDate = expirationDate ?? maturityDate,
				SecurityType = _securityTypes[securityType],
				UnderlyingSecurityCode = exchangeRoot,
				ExtensionInfo = new Dictionary<object, object>
				{
					{ "SIC", sic },
					{ "NAICS", naics },
				}
			};

			yield return new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = CurrentTime.Convert(TimeHelper.Est)
			}
			.TryAdd(Level1Fields.HistoricalVolatility, historicalVolatility)
			.TryAdd(Level1Fields.Beta, beta)
			.TryAdd(Level1Fields.PriceEarnings, pe);
		}

		private IEnumerable<Message> ToSecurityUpdateMessage(string value)
		{
			var parts = value.SplitByComma();

			var index = 0;

			var secCode = parts[index++];
			var exchangeId = int.Parse(parts[index++], NumberStyles.HexNumber);
			var tradeBoard = parts[index++].To<int?>();
			var bidBoard = parts[index++].To<int?>();
			var askBoard = parts[index++].To<int?>();

			var l1Messages = new[] { exchangeId, tradeBoard, bidBoard, askBoard }
				.Where(id => id != null)
				.Select(id => id.Value)
				.Distinct()
				.ToDictionary(boardId => boardId, boardId => new Level1ChangeMessage
				{
					SecurityId = CreateSecurityId(secCode, boardId)
				});

			DateTime? lastTradeDate = null;
			TimeSpan? lastTradeTime = null;
			decimal? lastTradePrice = null;
			decimal? lastTradeVolume = null;
			long? lastTradeId = null;

			var types = new HashSet<Level1Fields>(Enumerator.GetValues<Level1Fields>());
			var messageContentsIndex = Level1Columns.IndexOf(Level1ColumnRegistry.MessageContents);

			if (messageContentsIndex != -1)
				types.Exclude(parts[messageContentsIndex + index]);

			if (tradeBoard == null)
			{
				types.Remove(Level1Fields.LastTradeId);
				types.Remove(Level1Fields.LastTradeTime);
				types.Remove(Level1Fields.LastTradePrice);
				types.Remove(Level1Fields.LastTradeVolume);
			}

			if (bidBoard == null)
			{
				types.Remove(Level1Fields.BestBidTime);
				types.Remove(Level1Fields.BestBidPrice);
				types.Remove(Level1Fields.BestBidVolume);
			}

			if (askBoard == null)
			{
				types.Remove(Level1Fields.BestAskTime);
				types.Remove(Level1Fields.BestAskPrice);
				types.Remove(Level1Fields.BestAskVolume);
			}

			foreach (var column in Level1Columns)
			{
				var colValue = parts[index++];

				if (colValue.IsEmpty())
					continue;

				//colValue = colValue.To(column.Type);

				switch (column.Field)
				{
					case IQFeedLevel1Column.DefaultField:
					{
						var typedColValue = column.Convert(colValue);

						if (typedColValue != null)
							l1Messages[exchangeId].AddValue(column.Name, typedColValue);

						break;
					}

					case Level1Fields.LastTradeId:
						if (types.Contains(column.Field))
						{
							lastTradeId = colValue.To<long?>();

							if (lastTradeId != null && lastTradeId != 0)
								l1Messages[tradeBoard.Value].Add(column.Field, lastTradeId.Value);
						}
						break;

					case Level1Fields.LastTradeTime:
						if (types.Contains(column.Field))
						{
							if (column == Level1ColumnRegistry.LastDate)
								lastTradeDate = colValue.TryToDateTime(column.Format);
							else if (column == Level1ColumnRegistry.LastTradeTime)
								lastTradeTime = column.ConvertToTimeSpan(colValue);

							if (lastTradeDate.HasValue && lastTradeTime.HasValue)
							{
								var l1Msg = l1Messages[tradeBoard.Value];
								l1Msg.ServerTime = (lastTradeDate.Value + lastTradeTime.Value).ApplyTimeZone(TimeHelper.Est);
								l1Msg.Add(Level1Fields.LastTradeTime, l1Msg.ServerTime);
							}
						}
						break;

					case Level1Fields.LastTradePrice:
					case Level1Fields.LastTradeVolume:
						if (types.Contains(column.Field))
						{
							var decValue = colValue.To<decimal?>();

							l1Messages[tradeBoard.Value].TryAdd(column.Field, decValue);

							if (column == Level1ColumnRegistry.LastTradePrice)
								lastTradePrice = decValue;
							else// if (column == SessionHolder.Level1ColumnRegistry.LastTradeVolume)
								lastTradeVolume = decValue;
						}
						break;

					case Level1Fields.BestBidTime:
						if (types.Contains(column.Field))
						{
							var typedColValue = column.ConvertToTimeSpan(colValue);

							if (typedColValue != null)
							{
								var l1Msg = l1Messages[bidBoard.Value];
								l1Msg.ServerTime = (DateTime.Today + typedColValue.Value).ApplyTimeZone(TimeHelper.Est);
								l1Msg.Add(Level1Fields.BestBidTime, l1Msg.ServerTime);
							}
						}
						break;
					case Level1Fields.BestBidPrice:
					case Level1Fields.BestBidVolume:
						if (types.Contains(column.Field))
							l1Messages[bidBoard.Value].TryAdd(column.Field, colValue.To<decimal?>());
						break;

					case Level1Fields.BestAskTime:
						if (types.Contains(column.Field))
						{
							var typedColValue = column.ConvertToTimeSpan(colValue);

							if (typedColValue != null)
							{
								var l1Msg = l1Messages[askBoard.Value];
								l1Msg.ServerTime = (DateTime.Today + typedColValue.Value).ApplyTimeZone(TimeHelper.Est);
								l1Msg.Add(Level1Fields.BestAskTime, l1Msg.ServerTime);
							}
						}
						break;
					case Level1Fields.BestAskPrice:
					case Level1Fields.BestAskVolume:
						if (types.Contains(column.Field))
							l1Messages[askBoard.Value].TryAdd(column.Field, colValue.To<decimal?>());
						break;

					case Level1Fields.OpenInterest:
					case Level1Fields.OpenPrice:
					case Level1Fields.HighPrice:
					case Level1Fields.LowPrice:
					case Level1Fields.ClosePrice:
					case Level1Fields.SettlementPrice:
					case Level1Fields.VWAP:
						if (types.Contains(column.Field))
							l1Messages[exchangeId].TryAdd(column.Field, colValue.To<decimal?>());
						break;

					default:
						if (types.Contains(column.Field))
						{
							var typedColValue = column.Convert(colValue);

							if (typedColValue != null)
								l1Messages[exchangeId].Add(column.Field, typedColValue);
						}
						break;
				}
			}

			foreach (var l1Msg in l1Messages.Values)
			{
				if (l1Msg.Changes.Count <= 0)
					continue;

				yield return new SecurityMessage { SecurityId = l1Msg.SecurityId };

				if (l1Msg.ServerTime.IsDefault())
					l1Msg.ServerTime = CurrentTime.Convert(TimeHelper.Est);

				yield return l1Msg;
			}

			if (!types.Contains(Level1Fields.LastTrade) || !lastTradeDate.HasValue || !lastTradeTime.HasValue
					|| !lastTradeId.HasValue || !lastTradePrice.HasValue || !lastTradeVolume.HasValue)
				yield break;

			yield return new ExecutionMessage
			{
				SecurityId = l1Messages[tradeBoard.Value].SecurityId,
				TradeId = lastTradeId.Value,
				ServerTime = (lastTradeDate.Value + lastTradeTime.Value).ApplyTimeZone(TimeHelper.Est),
				TradePrice = lastTradePrice.Value,
				TradeVolume = lastTradeVolume.Value,
				ExecutionType = ExecutionTypes.Tick,
			};
		}

		private static NewsMessage ToNewsMessage(string value)
		{
			var parts = value.SplitByComma();

			var timeFormat = parts[3].Contains(' ') ? "yyyyMMdd HHmmss" : "yyyyMMddHHmmss";

			var msg = new NewsMessage
			{
				Source = parts[0],
				Id = parts[1],
				ServerTime = parts[3].ToDateTime(timeFormat).ApplyTimeZone(TimeHelper.Est),
				Headline = parts[4],
			};

			// news received without associated instrument
			if (parts[2].IsEmpty())
				return msg;

			var symbols = parts[2].Split(":");

			if (symbols.Length > 0)
			{
				msg.ExtensionInfo = new Dictionary<object, object>
				{
					{ "Symbols", symbols }
				};
			}

			return msg;
		}

		private static Level1ChangeMessage ToLevel1(string str, SecurityId securityId)
		{
			var parts = str.SplitByComma();

			return new Level1ChangeMessage
			{
				SecurityId = securityId,
				ServerTime = parts[0].ToDateTime("yyyy-MM-dd HH:mm:ss.fff").ApplyTimeZone(TimeHelper.Est),
			}
			.TryAdd(Level1Fields.LastTradeId, parts[6].IsEmpty() ? 0 : parts[6].To<long>())
			.TryAdd(Level1Fields.LastTradePrice, parts[1].To<decimal?>())
			.TryAdd(Level1Fields.LastTradeVolume, parts[2].To<decimal?>())
			.TryAdd(Level1Fields.BestBidPrice, parts[4].To<decimal?>())
			.TryAdd(Level1Fields.BestAskPrice, parts[5].To<decimal?>());
		}

		private static Level1ChangeMessage ToLevel2(string value)
		{
			var parts = value.SplitByComma();

			var isBidValid = parts[10] == "T";
			var isAskValid = parts[11] == "T";

			if (!isBidValid && !isAskValid)
				return null;

			var date = parts[7].ToDateTime("yyyy-MM-dd");

			var l1Msg = new Level1ChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = parts[0],
					BoardCode = parts[1]
				},
			};

			// http://www.iqfeed.net/dev/api/docs/ConditionCodes.cfm
			l1Msg.Add(Level1Fields.IsSystem, parts[8] == "52");

			if (isAskValid)
			{
				l1Msg.ServerTime = date.Add(parts[9].To<TimeSpan>()).ApplyTimeZone(TimeHelper.Est);

				l1Msg
					.TryAdd(Level1Fields.BestAskPrice, parts[3].To<decimal>())
					.TryAdd(Level1Fields.BestAskVolume, parts[5].To<decimal>())
					.Add(Level1Fields.BestAskTime, l1Msg.ServerTime);
			}

			if (isBidValid)
			{
				var bidTime = date.Add(parts[6].To<TimeSpan>()).ApplyTimeZone(TimeHelper.Est);

				if (bidTime > l1Msg.ServerTime)
					l1Msg.ServerTime = bidTime;

				l1Msg
					.TryAdd(Level1Fields.BestBidPrice, parts[2].To<decimal>())
					.TryAdd(Level1Fields.BestBidVolume, parts[4].To<decimal>())
					.Add(Level1Fields.BestBidTime, bidTime);
			}

			return l1Msg;
		}

		private SecurityId CreateSecurityId(string secCode, int marketId)
		{
			return new SecurityId
			{
				SecurityCode = secCode,
				BoardCode = _markets[marketId],
			};
		}

		private static readonly Func<string[], CandleMessage> _candleParser =
			parts => new TimeFrameCandleMessage
			{
				CloseTime = parts[0].ToDateTime("yyyy-MM-dd").ApplyTimeZone(TimeHelper.Est),
				HighPrice = parts[1].To<decimal>(),
				LowPrice = parts[2].To<decimal>(),
				OpenPrice = parts[3].To<decimal>(),
				ClosePrice = parts[4].To<decimal>(),
				TotalVolume = parts[5].To<decimal>(),
				OpenInterest = parts[6] != "0" ? parts[6].To<decimal?>() : null,
			};

		private static readonly Func<string[], CandleMessage> _candleIntradayParser =
			parts => new TimeFrameCandleMessage
			{
				CloseTime = parts[0].ToDateTime("yyyy-MM-dd HH:mm:ss").ApplyTimeZone(TimeHelper.Est),
				HighPrice = parts[1].To<decimal>(),
				LowPrice = parts[2].To<decimal>(),
				OpenPrice = parts[3].To<decimal>(),
				ClosePrice = parts[4].To<decimal>(),
				TotalVolume = parts[6].To<decimal>(),
				TotalTicks = parts[7].To<int?>() ?? 0,
			};

		private static readonly Func<string[], CandleMessage> _candleStreamingParser =
			parts => new TimeFrameCandleMessage
			{
				CloseTime = parts[2].ToDateTime("yyyy-MM-dd HH:mm:ss").ApplyTimeZone(TimeHelper.Est),
				OpenPrice = parts[3].To<decimal>(),
				HighPrice = parts[4].To<decimal>(),
				LowPrice = parts[5].To<decimal>(),
				ClosePrice = parts[6].To<decimal>(),
				TotalVolume = parts[8].To<decimal>(),
				TotalTicks = parts[9].To<int?>() ?? 0,
				State = parts[1][1] == 'U' ? CandleStates.Active : CandleStates.Finished
			};
	}
}