namespace StockSharp.Algo.Export;

using Ecng.Interop;

/// <summary>
/// The export into Excel.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExcelExporter"/>.
/// </remarks>
/// <param name="provider">Excel provider.</param>
/// <param name="dataType">Data type info.</param>
/// <param name="isCancelled">The processor, returning process interruption sign.</param>
/// <param name="fileName">The path to file.</param>
/// <param name="breaked">The processor, which will be called if maximal value of strings is exceeded.</param>
public class ExcelExporter(IExcelWorkerProvider provider, DataType dataType, Func<int, bool> isCancelled, string fileName, Action breaked) : BaseExporter(dataType, isCancelled, fileName)
{
	private readonly IExcelWorkerProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
	private readonly Action _breaked = breaked ?? throw new ArgumentNullException(nameof(breaked));

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportOrderLog(IEnumerable<ExecutionMessage> messages)
	{
		return Do(worker =>
		{
			worker
				.SetCell(0, 0, LocalizedStrings.Id).SetStyle(0, typeof(string))
				.SetCell(1, 0, LocalizedStrings.Time).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff zzz")
				.SetCell(2, 0, LocalizedStrings.Price).SetStyle(2, typeof(decimal))
				.SetCell(3, 0, LocalizedStrings.Volume).SetStyle(3, typeof(decimal))
				.SetCell(4, 0, LocalizedStrings.Direction)
				.SetCell(5, 0, LocalizedStrings.Action)
				.SetCell(6, 0, LocalizedStrings.Type)
				.SetCell(7, 0, LocalizedStrings.System)
				.SetCell(8, 0, LocalizedStrings.IdTrade).SetStyle(8, typeof(string))
				.SetCell(9, 0, LocalizedStrings.TradePrice).SetStyle(9, typeof(decimal))
				.SetCell(10, 0, LocalizedStrings.OITrade).SetStyle(10, typeof(decimal));

			//worker.SetConditionalFormatting(4, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Buy), null, Colors.Green);
			//worker.SetConditionalFormatting(4, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Sell), null, Colors.Red);

			var row = 1;
			var lastTime = default(DateTimeOffset?);

			foreach (var message in messages)
			{
				worker
					.SetCell(0, row, message.OrderId == null ? message.OrderStringId : message.OrderId.To<string>())
					.SetCell(1, row, message.ServerTime)
					.SetCell(2, row, message.OrderPrice)
					.SetCell(3, row, message.OrderVolume)
					.SetCell(4, row, message.Side)
					.SetCell(5, row, message.OrderState)
					.SetCell(6, row, message.TimeInForce)
					.SetCell(7, row, message.IsSystem);

				if (message.TradePrice != null)
				{
					worker
						.SetCell(8, row, message.TradeId == null ? message.TradeStringId : message.TradeId.To<string>())
						.SetCell(9, row, message.TradePrice)
						.SetCell(10, row, message.OpenInterest);
				}

				lastTime = message.ServerTime;

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTicks(IEnumerable<ExecutionMessage> messages)
	{
		return Do(worker =>
		{
			worker
				.SetCell(0, 0, LocalizedStrings.Id).SetStyle(0, typeof(string))
				.SetCell(1, 0, LocalizedStrings.Time).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff zzz")
				.SetCell(2, 0, LocalizedStrings.Price).SetStyle(2, typeof(decimal))
				.SetCell(3, 0, LocalizedStrings.Volume).SetStyle(3, typeof(decimal))
				.SetCell(4, 0, LocalizedStrings.Direction)
				.SetCell(5, 0, LocalizedStrings.OI).SetStyle(5, typeof(decimal))
				.SetCell(6, 0, "UP_DOWN").SetStyle(5, typeof(bool))
				.SetCell(7, 0, LocalizedStrings.Currency);

			//worker.SetConditionalFormatting(4, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Buy), null, Colors.Green);
			//worker.SetConditionalFormatting(4, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Sell), null, Colors.Red);

			var row = 1;
			var lastTime = default(DateTimeOffset?);

			foreach (var message in messages)
			{
				worker
					.SetCell(0, row, message.TradeId == null ? message.TradeStringId : message.TradeId.To<string>())
					.SetCell(1, row, message.ServerTime)
					.SetCell(2, row, message.TradePrice)
					.SetCell(3, row, message.TradeVolume)
					.SetCell(4, row, message.OriginSide)
					.SetCell(5, row, message.OpenInterest)
					.SetCell(6, row, message.IsUpTick)
					.SetCell(7, row, message.Currency);

				lastTime = message.ServerTime;

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTransactions(IEnumerable<ExecutionMessage> messages)
	{
		return Do(worker =>
		{
			worker
				.SetCell(0, 0, LocalizedStrings.Time).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff zzz")
				.SetCell(1, 0, LocalizedStrings.Portfolio)
				.SetCell(2, 0, LocalizedStrings.TransactionId)
				.SetCell(3, 0, LocalizedStrings.OrderId)
				.SetCell(4, 0, LocalizedStrings.Price).SetStyle(2, typeof(decimal))
				.SetCell(5, 0, LocalizedStrings.Volume).SetStyle(3, typeof(decimal))
				.SetCell(6, 0, LocalizedStrings.Balance).SetStyle(3, typeof(decimal))
				.SetCell(7, 0, LocalizedStrings.Direction)
				.SetCell(8, 0, LocalizedStrings.OrderType)
				.SetCell(9, 0, LocalizedStrings.OrderStateDesc)
				.SetCell(10, 0, LocalizedStrings.Trade)
				.SetCell(11, 0, LocalizedStrings.TradePrice).SetStyle(3, typeof(decimal));

			//worker.SetConditionalFormatting(7, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Buy), null, Colors.Green);
			//worker.SetConditionalFormatting(7, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Sell), null, Colors.Red);

			//worker.SetConditionalFormatting(9, ComparisonOperator.Equal, "\"{0}\"".Put(OrderStates.Active), null, Colors.Blue);
			//worker.SetConditionalFormatting(9, ComparisonOperator.Equal, "\"{0}\"".Put(OrderStates.Done), null, Colors.Green);
			//worker.SetConditionalFormatting(9, ComparisonOperator.Equal, "\"{0}\"".Put(OrderStates.Failed), null, Colors.Red);

			var row = 1;
			var lastTime = default(DateTimeOffset?);

			foreach (var message in messages)
			{
				worker
					.SetCell(0, row, message.ServerTime)
					.SetCell(1, row, message.PortfolioName)
					.SetCell(2, row, message.TransactionId)
					.SetCell(3, row, message.OrderId == null ? message.OrderStringId : message.OrderId.To<string>())
					.SetCell(4, row, message.OrderPrice)
					.SetCell(5, row, message.OrderVolume)
					.SetCell(6, row, message.Balance)
					.SetCell(7, row, message.Side)
					.SetCell(8, row, message.OrderType)
					.SetCell(9, row, message.OrderState)
					.SetCell(10, row, message.TradeId == null ? message.TradeStringId : message.TradeId.To<string>())
					.SetCell(11, row, message.TradePrice)
					.SetCell(12, row, message.HasOrderInfo);

				lastTime = message.ServerTime;

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<QuoteChangeMessage> messages)
	{
		return Do(worker =>
		{
			var count = 0;
			var lastTime = default(DateTimeOffset?);

			var rowIndex = 0;

			foreach (var message in messages)
			{
				worker
					.SetCell(0, rowIndex, LocalizedStrings.Time)
					.SetCell(1, rowIndex, message.ServerTime.ToString());

				var columnIndex = 0;

				var bids = new HashSet<QuoteChange>(message.Bids);

				foreach (var quote in message.Bids.Concat(message.Asks).OrderByDescending(q => q.Price))
				{
					worker
						.SetCell(columnIndex, rowIndex + (bids.Contains(quote) ? 1 : 3), quote.Price)
						.SetCell(columnIndex, rowIndex + 2, quote.Volume)
						.SetCell(columnIndex, rowIndex + 4, quote.OrdersCount);

					if (quote.Condition != default)
						worker.SetCell(columnIndex, rowIndex + 5, quote.Condition.GetDisplayName());

					columnIndex++;

					count++;
				}

				lastTime = message.ServerTime;

				rowIndex += 6; // 1 header + 2 (bids/asks price rows) + 1 volume + 1 ordersCount + 1 condition = 6

				if (!Check(rowIndex))
					break;
			}

			return (count, lastTime);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<Level1ChangeMessage> messages)
	{
		return Do(worker =>
		{
			var columns = new Dictionary<Level1Fields, int>();
			//{
			//	{ Level1Fields.LastTradeId, 1 },
			//	{ Level1Fields.LastTradePrice, 2 },
			//	{ Level1Fields.LastTradeVolume, 3 },
			//	{ Level1Fields.LastTradeOrigin, 4 },
			//	{ Level1Fields.BestBidPrice, 5 },
			//	{ Level1Fields.BestBidVolume, 6 },
			//	{ Level1Fields.BestAskPrice, 7 },
			//	{ Level1Fields.BestAskVolume, 8 },
			//	{ Level1Fields.StepPrice, 9 },
			//	{ Level1Fields.OpenInterest, 10 },
			//	{ Level1Fields.TheorPrice, 11 },
			//	{ Level1Fields.ImpliedVolatility, 12 },
			//	{ Level1Fields.OpenPrice, 13 },
			//	{ Level1Fields.HighPrice, 14 },
			//	{ Level1Fields.LowPrice, 15 },
			//	{ Level1Fields.ClosePrice, 16 },
			//	{ Level1Fields.Volume, 17 },
			//};

			worker
				.SetCell(0, 0, LocalizedStrings.Time).SetStyle(0, "yyyy-MM-dd HH:mm:ss.fff");

			var row = 1;
			var lastTime = default(DateTimeOffset?);

			foreach (var message in messages)
			{
				worker.SetCell(0, row, message.LocalTime);

				foreach (var pair in message.Changes)
				{
					var field = pair.Key;

					if (!columns.TryGetValue(field, out var columnIndex))
					{
						columnIndex = columns.Count + 1; // reserve column 0 for time
						columns.Add(field, columnIndex);

						worker.SetCell(columnIndex, 0, field.GetDisplayName());
						ApplyCellStyle(worker, field, columnIndex);
					}

					worker.SetCell(columns[field], row, pair.Value);
				}

				lastTime = message.ServerTime;

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	private static void ApplyCellStyle(IExcelWorker worker, Level1Fields field, int column)
	{
		var type = field.ToType();

		if (type != null && !type.IsEnum)
			worker.SetStyle(column, type);
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<PositionChangeMessage> messages)
	{
		return Do(worker =>
		{
			var columns = new Dictionary<PositionChangeTypes, int>();

			worker
				.SetCell(0, 0, LocalizedStrings.Time).SetStyle(0, "yyyy-MM-dd HH:mm:ss.fff");

			var row = 1;
			var lastTime = default(DateTimeOffset?);

			foreach (var message in messages)
			{
				worker.SetCell(0, row, message.LocalTime);

				foreach (var pair in message.Changes)
				{
					var type = pair.Key;

					if (!columns.TryGetValue(type, out var columnIndex))
					{
						columnIndex = columns.Count + 1; // reserve column 0 for time
						columns.Add(type, columnIndex);

						worker.SetCell(columnIndex, 0, type.GetDisplayName());
						ApplyCellStyle(worker, type, columnIndex);
					}

					worker.SetCell(columns[type], row, pair.Value);
				}

				lastTime = message.ServerTime;

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<IndicatorValue> values)
	{
		return Do(worker =>
		{
			var row = 0;

			worker
				.SetCell(0, row, LocalizedStrings.Time)
				.SetCell(1, row, LocalizedStrings.Value);

			row++;

			var lastTime = default(DateTimeOffset?);

			foreach (var value in values)
			{
				worker.SetCell(0, row, value.Time);

				var col = 1;
				foreach (var indVal in value.ValuesAsDecimal)
					worker.SetCell(col++, row, indVal);

				lastTime = value.Time;

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	private static void ApplyCellStyle(IExcelWorker worker, PositionChangeTypes type, int column)
	{
		switch (type)
		{
			case PositionChangeTypes.Currency:
			case PositionChangeTypes.State:
				worker.SetStyle(column, typeof(string));
				break;
			default:
				worker.SetStyle(column, typeof(decimal));
				break;
		}
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<CandleMessage> messages)
	{
		return Do(worker =>
		{
			var row = 0;

			worker
				.SetCell(0, row, LocalizedStrings.Time).SetStyle(0, "yyyy-MM-dd HH:mm:ss.fff")
				.SetCell(1, row, "O").SetStyle(2, typeof(decimal))
				.SetCell(2, row, "H").SetStyle(3, typeof(decimal))
				.SetCell(3, row, "L").SetStyle(4, typeof(decimal))
				.SetCell(4, row, "C").SetStyle(5, typeof(decimal))
				.SetCell(5, row, "V").SetStyle(6, typeof(decimal))
				.SetCell(6, row, LocalizedStrings.OI).SetStyle(7, typeof(decimal));

			row++;

			var lastTime = default(DateTimeOffset?);

			foreach (var candle in messages)
			{
				worker
					.SetCell(0, row, candle.OpenTime)
					.SetCell(1, row, candle.OpenPrice)
					.SetCell(2, row, candle.HighPrice)
					.SetCell(3, row, candle.LowPrice)
					.SetCell(4, row, candle.ClosePrice)
					.SetCell(5, row, candle.TotalVolume)
					.SetCell(6, row, candle.OpenInterest);

				lastTime = candle.OpenTime;

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<NewsMessage> messages)
	{
		return Do(worker =>
		{
			var row = 0;

			worker
				.SetCell(0, row, LocalizedStrings.Id).SetStyle(0, typeof(string))
				.SetCell(1, row, LocalizedStrings.Time).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff")
				.SetCell(2, row, LocalizedStrings.Security).SetStyle(2, typeof(string))
				.SetCell(3, row, LocalizedStrings.Board).SetStyle(3, typeof(string))
				.SetCell(4, row, LocalizedStrings.Header).SetStyle(4, typeof(string))
				.SetCell(5, row, LocalizedStrings.Text).SetStyle(5, typeof(string))
				.SetCell(6, row, LocalizedStrings.Source).SetStyle(6, typeof(string))
				.SetCell(7, row, LocalizedStrings.Link).SetStyle(7, typeof(string));

			row++;

			var lastTime = default(DateTimeOffset?);

			foreach (var n in messages)
			{
				worker
					.SetCell(0, row, n.Id)
					.SetCell(1, row, n.ServerTime)
					.SetCell(2, row, n.SecurityId?.SecurityCode)
					.SetCell(3, row, n.BoardCode)
					.SetCell(4, row, n.Headline)
					.SetCell(5, row, n.Story)
					.SetCell(6, row, n.Source)
					.SetCell(7, row, n.Url);

				lastTime = n.ServerTime;

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<SecurityMessage> messages)
	{
		return Do(worker =>
		{
			var colIndex = 0;

			worker
				.SetCell(colIndex, 0, LocalizedStrings.Code).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.Board).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.Name).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.ShortName).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.PriceStep).SetStyle(colIndex++, typeof(decimal))
				.SetCell(colIndex, 0, LocalizedStrings.VolumeStep).SetStyle(colIndex++, typeof(decimal))
				.SetCell(colIndex, 0, LocalizedStrings.MinVolume).SetStyle(colIndex++, typeof(decimal))
				.SetCell(colIndex, 0, LocalizedStrings.MaxVolume).SetStyle(colIndex++, typeof(decimal))
				.SetCell(colIndex, 0, LocalizedStrings.Lot).SetStyle(colIndex++, typeof(decimal))
				.SetCell(colIndex, 0, LocalizedStrings.Type).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.Decimals).SetStyle(colIndex++, typeof(decimal))
				.SetCell(colIndex, 0, LocalizedStrings.OptionType).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.Strike).SetStyle(colIndex++, typeof(decimal))
				.SetCell(colIndex, 0, LocalizedStrings.UnderlyingAsset).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.UnderlyingSecurityType).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.UnderlyingMinVolume).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.ExpiryDate).SetStyle(colIndex++, "yyyy-MM-dd")
				.SetCell(colIndex, 0, LocalizedStrings.SettlementDate).SetStyle(colIndex++, "yyyy-MM-dd")
				.SetCell(colIndex, 0, LocalizedStrings.IssueSize).SetStyle(colIndex++, typeof(decimal))
				.SetCell(colIndex, 0, LocalizedStrings.IssueDate).SetStyle(colIndex++, "yyyy-MM-dd")
				.SetCell(colIndex, 0, LocalizedStrings.Currency).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.CfiCode).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.Shortable).SetStyle(colIndex++, typeof(bool))
				.SetCell(colIndex, 0, LocalizedStrings.Basket).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.Expression).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.FaceValue).SetStyle(colIndex++, typeof(decimal))
				.SetCell(colIndex, 0, LocalizedStrings.OptionStyle).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.Settlement).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.Code).SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, LocalizedStrings.Board).SetStyle(colIndex++, typeof(string))

				.SetCell(colIndex, 0, "Bloomberg").SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, "CUSIP").SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, "IQFeed").SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, "InteractiveBrokers").SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, "ISIN").SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, "Plaza").SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, "RIC").SetStyle(colIndex++, typeof(string))
				.SetCell(colIndex, 0, "SEDOL").SetStyle(colIndex, typeof(string));

			var rowIndex = 1;

			var lastTime = default(DateTimeOffset?);

			foreach (var security in messages)
			{
				colIndex = 0;

				worker
					.SetCell(colIndex++, rowIndex, security.SecurityId.SecurityCode)
					.SetCell(colIndex++, rowIndex, security.SecurityId.BoardCode)
					.SetCell(colIndex++, rowIndex, security.Name)
					.SetCell(colIndex++, rowIndex, security.ShortName)
					.SetCell(colIndex++, rowIndex, security.PriceStep)
					.SetCell(colIndex++, rowIndex, security.VolumeStep)
					.SetCell(colIndex++, rowIndex, security.MinVolume)
					.SetCell(colIndex++, rowIndex, security.MaxVolume)
					.SetCell(colIndex++, rowIndex, security.Multiplier)
					.SetCell(colIndex++, rowIndex, security.SecurityType?.GetDisplayName() ?? string.Empty)
					.SetCell(colIndex++, rowIndex, security.Decimals)
					.SetCell(colIndex++, rowIndex, security.OptionType?.GetDisplayName() ?? string.Empty)
					.SetCell(colIndex++, rowIndex, security.Strike)
					.SetCell(colIndex++, rowIndex, security.BinaryOptionType)
					.SetCell(colIndex++, rowIndex, security.UnderlyingSecurityId.ToStringId(nullIfEmpty: true))
					.SetCell(colIndex++, rowIndex, security.UnderlyingSecurityType?.GetDisplayName() ?? string.Empty)
					.SetCell(colIndex++, rowIndex, security.UnderlyingSecurityMinVolume)
					.SetCell(colIndex++, rowIndex, security.ExpiryDate)
					.SetCell(colIndex++, rowIndex, security.SettlementDate)
					.SetCell(colIndex++, rowIndex, security.IssueSize)
					.SetCell(colIndex++, rowIndex, security.IssueDate)
					.SetCell(colIndex++, rowIndex, security.Currency?.GetDisplayName() ?? string.Empty)
					.SetCell(colIndex++, rowIndex, security.CfiCode)
					.SetCell(colIndex++, rowIndex, security.Shortable)
					.SetCell(colIndex++, rowIndex, security.BasketCode)
					.SetCell(colIndex++, rowIndex, security.BasketExpression)
					.SetCell(colIndex++, rowIndex, security.FaceValue)
					.SetCell(colIndex++, rowIndex, security.OptionStyle)
					.SetCell(colIndex++, rowIndex, security.SettlementType)
					.SetCell(colIndex++, rowIndex, security.PrimaryId.SecurityCode)
					.SetCell(colIndex++, rowIndex, security.PrimaryId.BoardCode)
					.SetCell(colIndex++, rowIndex, security.SecurityId.Bloomberg)
					.SetCell(colIndex++, rowIndex, security.SecurityId.Cusip)
					.SetCell(colIndex++, rowIndex, security.SecurityId.IQFeed)
					.SetCell(colIndex++, rowIndex, security.SecurityId.InteractiveBrokers)
					.SetCell(colIndex++, rowIndex, security.SecurityId.Isin)
					.SetCell(colIndex++, rowIndex, security.SecurityId.Plaza)
					.SetCell(colIndex++, rowIndex, security.SecurityId.Ric)
					.SetCell(colIndex++, rowIndex, security.SecurityId.Sedol);

				rowIndex++;

				if (!Check(rowIndex))
					break;
			}

			return (rowIndex - 1, lastTime);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardStateMessage> messages)
	{
		return Do(worker =>
		{
			var row = 0;
			worker
				.SetCell(0, row, LocalizedStrings.Time).SetStyle(0, "yyyy-MM-dd HH:mm:ss.fff")
				.SetCell(1, row, LocalizedStrings.Board).SetStyle(1, typeof(string))
				.SetCell(2, row, LocalizedStrings.State).SetStyle(2, typeof(string));

			row++;

			var lastTime = default(DateTimeOffset?);

			foreach (var msg in messages)
			{
				worker
					.SetCell(0, row, msg.ServerTime)
					.SetCell(1, row, msg.BoardCode)
					.SetCell(2, row, msg.State.ToString());

				lastTime = msg.ServerTime;

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardMessage> messages)
	{
		return Do(worker =>
		{
			var row = 0;
			worker
				.SetCell(0, row, LocalizedStrings.Code).SetStyle(0, typeof(string))
				.SetCell(1, row, LocalizedStrings.Exchange).SetStyle(1, typeof(string))
				.SetCell(2, row, LocalizedStrings.ExpiryDate).SetStyle(2, typeof(string))
				.SetCell(3, row, LocalizedStrings.TimeZone).SetStyle(3, typeof(string));

			row++;
			var lastTime = default(DateTimeOffset?);

			foreach (var msg in messages)
			{
				worker
					.SetCell(0, row, msg.Code)
					.SetCell(1, row, msg.ExchangeCode)
					.SetCell(2, row, msg.ExpiryTime.ToString())
					.SetCell(3, row, msg.TimeZone?.Id);

				if (!Check(++row))
					break;
			}

			return (row - 1, lastTime);
		});
	}

	private (int, DateTimeOffset?) Do(Func<IExcelWorker, (int, DateTimeOffset?)> action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		using (var stream = new FileStream(Path, FileMode.Create, FileAccess.Write))
		using (var worker = _provider.CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet(LocalizedStrings.Export);

			return action(worker);
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
