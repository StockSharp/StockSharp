namespace StockSharp.Algo.Storages.Csv;

/// <summary>
/// The level 1 serializer in the CSV format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Level1CsvSerializer"/>.
/// </remarks>
/// <param name="securityId">Security ID.</param>
/// <param name="encoding">Encoding.</param>
public class Level1CsvSerializer(SecurityId securityId, Encoding encoding) : CsvMarketDataSerializer<Level1ChangeMessage>(securityId, encoding)
{
	private static readonly Dictionary<Level1Fields, Type> _level1Fields = Enumerator.GetValues<Level1Fields>().ExcludeObsolete().OrderBy(l1 => (int)l1).ToDictionary(f => f, f => f.ToType());
	private static readonly string[] _reserved = new string[9];

	/// <inheritdoc />
	protected override void Write(CsvFileWriter writer, Level1ChangeMessage data, IMarketDataMetaInfo metaInfo)
	{
		var row = new List<string>();

		row.AddRange([data.ServerTime.WriteTime(), data.ServerTime.ToString("zzz")]);

		row.AddRange(data.BuildFrom.ToCsv());

		row.Add(data.SeqNum.DefaultAsNull().ToString());

		row.AddRange(_reserved);

		row.Add(_level1Fields.Count.To<string>());

		foreach (var pair in _level1Fields)
		{
			var field = pair.Key;

			if (pair.Value == typeof(DateTimeOffset))
			{
				var date = (DateTimeOffset?)data.TryGet(field);
				row.AddRange([date?.WriteDate(), date?.WriteTime(), date?.ToString("zzz")]);
			}
			else
			{
				row.Add(data.TryGet(field)?.ToString());
                }
		}

		writer.WriteRow(row);

		metaInfo.LastTime = data.ServerTime.UtcDateTime;
	}

	/// <inheritdoc />
	protected override Level1ChangeMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = SecurityId,
			ServerTime = reader.ReadTime(metaInfo.Date),
			BuildFrom = reader.ReadBuildFrom(),
			SeqNum = reader.ReadNullableLong() ?? 0L,
		};

		reader.Skip(_reserved.Length);

		var count = reader.ReadInt();

		foreach (var pair in _level1Fields.Take(count))
		{
			// backward compatibility
			if (reader.ColumnCurr == reader.ColumnCount)
				break;

			var field = pair.Key;

			if (pair.Value == typeof(DateTimeOffset))
			{
				var dtStr = reader.ReadString();

				if (dtStr != null)
				{
					level1.Changes.Add(field, (dtStr.ToDateTime() + reader.ReadString().ToTimeMls()).ToDateTimeOffset(TimeSpan.Parse(reader.ReadString().Remove("+"))));
				}
				else
				{
					reader.Skip(2);
				}
			}
			else if (pair.Value == typeof(int))
			{
				var value = reader.ReadNullableInt();

				if (value != null)
					level1.Changes.Add(field, value.Value);
			}
			else if (pair.Value == typeof(long))
			{
				var value = reader.ReadNullableLong();

				if (value != null)
					level1.Changes.Add(field, value.Value);
			}
			else if (pair.Value == typeof(bool))
			{
				var value = reader.ReadNullableBool();

				if (value != null)
					level1.Changes.Add(field, value.Value);
			}
			else if (pair.Value == typeof(SecurityStates))
			{
				var value = reader.ReadNullableEnum<SecurityStates>();

				if (value != null)
					level1.Changes.Add(field, value.Value);
			}
			else if (pair.Value == typeof(Sides))
			{
				var value = reader.ReadNullableEnum<Sides>();

				if (value != null)
					level1.Changes.Add(field, value.Value);
			}
			else if (pair.Value == typeof(string))
			{
				var value = reader.ReadString();

				if (!value.IsEmpty())
					level1.Changes.Add(field, value);
			}
			else
			{
				var value = reader.ReadNullableDecimal();

				if (value != null)
					level1.Changes.Add(field, value.Value);
			}
		}

		return level1;
	}
}