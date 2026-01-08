namespace StockSharp.Algo.Storages.Csv;

/// <summary>
/// The position change serializer in the CSV format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PositionCsvSerializer"/>.
/// </remarks>
/// <param name="securityId">Security ID.</param>
/// <param name="encoding">Encoding.</param>
public class PositionCsvSerializer(SecurityId securityId, Encoding encoding) : CsvMarketDataSerializer<PositionChangeMessage>(securityId, encoding)
{
	private static readonly PositionChangeTypes[] _types = [.. Enumerator.GetValues<PositionChangeTypes>().OrderBy(t => (int)t)];
	private static readonly string[] _reserved = new string[10];

	/// <inheritdoc />
	protected override ValueTask WriteAsync(CsvFileWriter writer, PositionChangeMessage data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
	{
		var row = new List<string>();

		row.AddRange(data.ServerTime.WriteTime().Concat(
		[
			data.PortfolioName,
			data.ClientCode,
			data.DepoName,
			data.LimitType.To<int?>().ToString(),
			data.Description,
			data.StrategyId,
			data.Side.To<int?>().ToString(),
		]));

		row.AddRange(data.BuildFrom.ToCsv());

		row.AddRange(_reserved);

		row.Add(_types.Length.To<string>());

		foreach (var type in _types)
		{
			var value = data.TryGet(type);

			if (type == PositionChangeTypes.ExpirationDate)
			{
				var date = (DateTime?)value;
				row.Add(date?.WriteDate());
				row.AddRange(date?.WriteTime() ?? new string[2]);
			}
			else
				row.Add(value?.ToString());
		}

		return writer.WriteRowAsync(row, cancellationToken);
	}

	/// <inheritdoc />
	protected override PositionChangeMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		var posMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId,
			ServerTime = reader.ReadTime(metaInfo.Date),
			PortfolioName = reader.ReadString(),
			ClientCode = reader.ReadString(),
			DepoName = reader.ReadString(),
			LimitType = reader.ReadNullableEnum<TPlusLimits>(),
			Description = reader.ReadString(),
			StrategyId = reader.ReadString(),
			Side = reader.ReadNullableEnum<Sides>(),
			BuildFrom = reader.ReadBuildFrom(),
		};

		reader.Skip(_reserved.Length);

		var count = reader.ReadInt();

		foreach (var type in _types.Take(count))
		{
			switch (type)
			{
				case PositionChangeTypes.Currency:
				{
					var currency = reader.ReadNullableEnum<CurrencyTypes>();

					if (currency != null)
						posMsg.Changes.Add(type, currency);

					break;
				}
				case PositionChangeTypes.State:
				{
					var state = reader.ReadNullableEnum<PortfolioStates>();

					if (state != null)
						posMsg.Changes.Add(type, state);

					break;
				}
				case PositionChangeTypes.ExpirationDate:
				{
					var dtStr = reader.ReadString();

					if (dtStr != null)
					{
						posMsg.Changes.Add(type, reader.ReadTime(dtStr.ToDateTime()));
					}
					else
					{
						reader.Skip(2);
					}

					break;
				}
				default:
				{
					var value = reader.ReadNullableDecimal();

					if (value != null)
						posMsg.Changes.Add(type, value);

					break;
				}
			}
		}

		return posMsg;
	}
}