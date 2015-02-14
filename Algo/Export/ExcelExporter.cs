namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Interop;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Экспорт в Excel.
	/// </summary>
	public class ExcelExporter : BaseExporter
	{
		private readonly Action _breaked;

		/// <summary>
		/// Создать <see cref="ExcelExporter"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Параметр данных.</param>
		/// <param name="isCancelled">Обработчик, возвращающий признак прерывания экспорта.</param>
		/// <param name="fileName">Путь к файлу.</param>
		/// <param name="breaked">Обработчик, который будет вызван в случае превышения максимального значения строчек.</param>
		public ExcelExporter(Security security, object arg, Func<int, bool> isCancelled, string fileName, Action breaked)
			: base(security, arg, isCancelled, fileName)
		{
			if (breaked == null)
				throw new ArgumentNullException("breaked");

			_breaked = breaked;
		}

		/// <summary>
		/// Экспортировать <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			switch ((ExecutionTypes)Arg)
			{
				case ExecutionTypes.Tick:
				{
					Do(worker =>
					{
						worker
							.SetCell(0, 0, LocalizedStrings.Id).SetStyle(0, typeof(string))
							.SetCell(1, 0, LocalizedStrings.Str219).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff zzz")
							.SetCell(2, 0, LocalizedStrings.Price).SetStyle(2, typeof(decimal))
							.SetCell(3, 0, LocalizedStrings.Volume).SetStyle(3, typeof(decimal))
							.SetCell(4, 0, LocalizedStrings.Str128)
							.SetCell(5, 0, LocalizedStrings.OI).SetStyle(5, typeof(decimal))
							.SetCell(6, 0, "UP_DOWN").SetStyle(5, typeof(bool));

						var index = 1;

						foreach (var message in messages)
						{
							worker
								.SetCell(0, index, message.TradeId == 0 ? message.TradeStringId : message.TradeId.To<string>())
								.SetCell(1, index, message.ServerTime)
								.SetCell(2, index, message.TradePrice)
								.SetCell(3, index, message.Volume)
								.SetCell(4, index, message.Side)
								.SetCell(5, index, message.OpenInterest)
								.SetCell(6, index, message.IsUpTick);

							index++;

							if (!Check(index))
								break;
						}
					});

					break;
				}
				case ExecutionTypes.OrderLog:
				{
					Do(worker =>
					{
						worker
							.SetCell(0, 0, LocalizedStrings.Id).SetStyle(0, typeof(string))
							.SetCell(1, 0, LocalizedStrings.Str219).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff zzz")
							.SetCell(2, 0, LocalizedStrings.Price).SetStyle(2, typeof(decimal))
							.SetCell(3, 0, LocalizedStrings.Volume).SetStyle(3, typeof(decimal))
							.SetCell(4, 0, LocalizedStrings.Str128)
							.SetCell(5, 0, LocalizedStrings.Str722)
							.SetCell(6, 0, LocalizedStrings.Type)
							.SetCell(7, 0, LocalizedStrings.Str342)
							.SetCell(8, 0, LocalizedStrings.Str723).SetStyle(8, typeof(string))
							.SetCell(9, 0, LocalizedStrings.Str724).SetStyle(9, typeof(decimal))
							.SetCell(10, 0, LocalizedStrings.Str725).SetStyle(10, typeof(decimal));

						var index = 1;

						foreach (var message in messages)
						{
							worker
								.SetCell(0, index, message.OrderId == 0 ? message.OrderStringId : message.OrderId.To<string>())
								.SetCell(1, index, message.ServerTime)
								.SetCell(2, index, message.Price)
								.SetCell(3, index, message.Volume)
								.SetCell(4, index, message.Side)
								.SetCell(5, index, message.OrderState)
								.SetCell(6, index, message.TimeInForce)
								.SetCell(7, index, message.IsSystem);

							if (message.TradePrice != 0)
							{
								worker
									.SetCell(8, index, message.TradeId == 0 ? message.TradeStringId : message.TradeId.To<string>())
									.SetCell(9, index, message.TradePrice)
									.SetCell(10, index, message.OpenInterest);
							}

							index++;

							if (!Check(index))
								break;
						}
					});

					break;
				}
				case ExecutionTypes.Order:
				{
					Do(worker =>
					{
						worker
							.SetCell(0, 0, LocalizedStrings.Str219).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff zzz")
							.SetCell(1, 0, LocalizedStrings.Portfolio)
							.SetCell(2, 0, LocalizedStrings.TransactionId)
							.SetCell(3, 0, LocalizedStrings.OrderId)
							.SetCell(4, 0, LocalizedStrings.Price).SetStyle(2, typeof(decimal))
							.SetCell(5, 0, LocalizedStrings.Volume).SetStyle(3, typeof(decimal))
							.SetCell(6, 0, LocalizedStrings.Str1325).SetStyle(3, typeof(decimal))
							.SetCell(7, 0, LocalizedStrings.Str128)
							.SetCell(8, 0, LocalizedStrings.Str132)
							.SetCell(9, 0, LocalizedStrings.Str134)
							.SetCell(10, 0, LocalizedStrings.Str506)
							.SetCell(11, 0, LocalizedStrings.TradePrice).SetStyle(3, typeof(decimal));

						var index = 1;

						foreach (var message in messages)
						{
							worker
								.SetCell(0, index, message.ServerTime)
								.SetCell(1, index, message.PortfolioName)
								.SetCell(2, index, message.TransactionId)
								.SetCell(3, index, message.OrderId == 0 ? message.OrderStringId : message.OrderId.To<string>())
								.SetCell(4, index, message.Price)
								.SetCell(5, index, message.Volume)
								.SetCell(6, index, message.Balance)
								.SetCell(7, index, message.Side)
								.SetCell(8, index, message.OrderType)
								.SetCell(9, index, message.OrderState)
								.SetCell(10, index, message.TradeId == 0 ? message.TradeStringId : message.TradeId.To<string>())
								.SetCell(11, index, message.TradePrice);

							index++;

							if (!Check(index))
								break;
						}
					});

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Экспортировать <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(worker =>
			{
				var rowIndex = 0;

				foreach (var message in messages)
				{
					worker
						.SetCell(0, rowIndex, LocalizedStrings.Str219)
						.SetCell(1, rowIndex, message.ServerTime);

					var columnIndex = 0;

					foreach (var quote in message.Bids.Concat(message.Asks).OrderByDescending(q => q.Price))
					{
						worker
							.SetCell(columnIndex, rowIndex + 1, quote.Price)
							.SetCell(columnIndex, rowIndex + 2, quote.Volume)
							.SetCell(columnIndex, rowIndex + 3, quote.Side == Sides.Buy ? "B" : "A");

						columnIndex++;
					}

					rowIndex += 4;

					if (!Check(rowIndex))
						break;
				}
			});
		}

		/// <summary>
		/// Экспортировать <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(worker =>
			{
				var columns = new Dictionary<Level1Fields, int>
				{
					{ Level1Fields.LastTradeId, 1 },
					{ Level1Fields.LastTradePrice, 2 },
					{ Level1Fields.LastTradeVolume, 3 },
					{ Level1Fields.LastTradeOrigin, 4 },
					{ Level1Fields.BestBidPrice, 5 },
					{ Level1Fields.BestBidVolume, 6 },
					{ Level1Fields.BestAskPrice, 7 },
					{ Level1Fields.BestAskVolume, 8 },
					{ Level1Fields.StepPrice, 9 },
					{ Level1Fields.OpenInterest, 10 },
					{ Level1Fields.TheorPrice, 11 },
					{ Level1Fields.ImpliedVolatility, 12 },
					{ Level1Fields.OpenPrice, 13 },
					{ Level1Fields.HighPrice, 14 },
					{ Level1Fields.LowPrice, 15 },
					{ Level1Fields.ClosePrice, 16 },
					{ Level1Fields.Volume, 17 },
				};

				worker
					.SetCell(0, 0, LocalizedStrings.Str219).SetStyle(0, "yyyy-MM-dd HH:mm:ss.fff");

				foreach (var field in Enumerator.GetValues<Level1Fields>())
				{
					var columnIndex = columns.TryGetValue2(field);

					if (columnIndex == null)
					{
						columnIndex = columns.Count;
						columns.Add(field, columnIndex.Value);
					}

					var cell = worker.SetCell(columns[field], 0, field.GetDisplayName());

					switch (field)
					{
						case Level1Fields.LastTrade:
						case Level1Fields.BestAsk:
						case Level1Fields.BestBid:
							continue;
						case Level1Fields.LastTradeId:
						case Level1Fields.BidsCount:
						case Level1Fields.AsksCount:
						case Level1Fields.TradesCount:
							cell.SetStyle(columns[field], typeof(long));
							break;
						case Level1Fields.LastTradeTime:
						case Level1Fields.BestAskTime:
						case Level1Fields.BestBidTime:
							cell.SetStyle(columns[field], typeof(DateTimeOffset));
							break;
						default:
							cell.SetStyle(columns[field], typeof(decimal));
							break;
					}
				}

				var row = 1;

				foreach (var message in messages)
				{
					worker.SetCell(0, row, message.LocalTime);

					foreach (var pair in message.Changes)
						worker.SetCell(columns[pair.Key], row, pair.Value);

					if (!Check(++row))
						break;
				}
			});
		}

		/// <summary>
		/// Экспортировать <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			Do(worker =>
			{
				worker
					.SetCell(0, 0, LocalizedStrings.Str726).SetStyle(0, "yyyy-MM-dd HH:mm:ss.fff")
					.SetCell(1, 0, LocalizedStrings.Str727).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff")
					.SetCell(2, 0, "O").SetStyle(2, typeof(decimal))
					.SetCell(3, 0, "H").SetStyle(3, typeof(decimal))
					.SetCell(4, 0, "L").SetStyle(4, typeof(decimal))
					.SetCell(5, 0, "C").SetStyle(5, typeof(decimal))
					.SetCell(6, 0, "V").SetStyle(6, typeof(decimal))
					.SetCell(7, 0, LocalizedStrings.OI).SetStyle(7, typeof(decimal));

				var index = 1;

				foreach (var candle in messages)
				{
					worker
						.SetCell(0, index, candle.OpenTime)
						.SetCell(1, index, candle.CloseTime)
						.SetCell(2, index, candle.OpenPrice)
						.SetCell(3, index, candle.HighPrice)
						.SetCell(4, index, candle.LowPrice)
						.SetCell(5, index, candle.ClosePrice)
						.SetCell(6, index, candle.TotalVolume)
						.SetCell(7, index, candle.OpenInterest);

					index++;

					if (!Check(index))
						break;
				}
			});
		}

		/// <summary>
		/// Экспортировать <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(worker =>
			{
				worker
					.SetCell(0, 0, LocalizedStrings.Id).SetStyle(0, typeof(string))
					.SetCell(1, 0, LocalizedStrings.Str219).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff")
					.SetCell(2, 0, LocalizedStrings.Security).SetStyle(2, typeof(string))
					.SetCell(3, 0, LocalizedStrings.Board).SetStyle(3, typeof(string))
					.SetCell(4, 0, LocalizedStrings.Str215).SetStyle(4, typeof(string))
					.SetCell(5, 0, LocalizedStrings.Str217).SetStyle(5, typeof(string))
					.SetCell(6, 0, LocalizedStrings.Str213).SetStyle(6, typeof(string))
					.SetCell(7, 0, LocalizedStrings.Str221).SetStyle(6, typeof(string));

				var index = 1;

				foreach (var n in messages)
				{
					worker
						.SetCell(0, index, n.Id)
						.SetCell(1, index, n.ServerTime)
						.SetCell(2, index, n.SecurityId.SecurityCode)
						.SetCell(3, index, n.BoardCode)
						.SetCell(4, index, n.Headline)
						.SetCell(5, index, n.Story)
						.SetCell(6, index, n.Source)
						.SetCell(7, index, n.Url);

					index++;

					if (!Check(index))
						break;
				}
			});
		}

		/// <summary>
		/// Экспортировать <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(worker =>
			{
				worker
					.SetCell(0, 0, LocalizedStrings.Code).SetStyle(0, typeof(string))
					.SetCell(1, 0, LocalizedStrings.Board).SetStyle(1, typeof(string))
					.SetCell(2, 0, LocalizedStrings.PriceStep).SetStyle(2, typeof(decimal))
					//.SetCell(3, 0, "Стоимость шага").SetStyle(3, typeof(decimal))
					.SetCell(3, 0, LocalizedStrings.Str365).SetStyle(4, typeof(decimal))
					.SetCell(4, 0, LocalizedStrings.Str330).SetStyle(5, typeof(decimal))
					.SetCell(5, 0, LocalizedStrings.Type).SetStyle(5, typeof(string))
					//.SetCell(6, 0, "Точность").SetStyle(6, typeof(decimal))
					.SetCell(6, 0, LocalizedStrings.Str551).SetStyle(5, typeof(string))
					.SetCell(7, 0, LocalizedStrings.Strike).SetStyle(6, typeof(decimal))
					.SetCell(8, 0, LocalizedStrings.UnderlyingAsset).SetStyle(6, typeof(string))
					.SetCell(9, 0, LocalizedStrings.ExpiryDate).SetStyle(6, "yyyy-MM-dd");

				var index = 1;

				foreach (var security in messages)
				{
					worker
						.SetCell(0, index, security.SecurityId.SecurityCode)
						.SetCell(1, index, security.SecurityId.BoardCode)
						.SetCell(2, index, security.PriceStep)
						//.SetCell(3, index, security.StepPrice)
						.SetCell(3, index, security.VolumeStep)
						.SetCell(4, index, security.Multiplier)
						.SetCell(5, index, security.SecurityType == null ? string.Empty : security.SecurityType.Value.GetDisplayName())
						//.SetCell(6, index, security.Decimals)
						.SetCell(6, index, security.OptionType == null ? string.Empty : security.OptionType.Value.GetDisplayName())
						.SetCell(7, index, security.Strike)
						.SetCell(8, index, security.UnderlyingSecurityCode)
						.SetCell(9, index, security.ExpiryDate);

					index++;

					if (!Check(index))
						break;
				}
			});
		}

		private void Do(Action<ExcelWorker> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			using (var worker = new ExcelWorker())
			{
				action(worker);
				worker.Save(Path);
			}
		}

		private bool Check(int index)
		{
			// http://office.microsoft.com/en-us/excel-help/excel-specifications-and-limits-HA103980614.aspx
			if (index < 1048576)
			//if (index < (ushort.MaxValue - 1))
			{
				return CanProcess();
			}
			else
			{
				_breaked();
				return false;
			}
		}
	}
}