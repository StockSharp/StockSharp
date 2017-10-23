namespace StockSharp.Algo.Storages.Csv
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The position change serializer in the CSV format.
	/// </summary>
	public class PositionCsvSerializer : CsvMarketDataSerializer<PositionChangeMessage>
	{
		private static readonly PositionChangeTypes[] _types = Enumerator.GetValues<PositionChangeTypes>().OrderBy(t => (int)t).ToArray();

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionCsvSerializer"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="encoding">Encoding.</param>
		public PositionCsvSerializer(SecurityId securityId, Encoding encoding = null)
			: base(securityId, encoding)
		{
		}

		/// <inheritdoc />
		protected override void Write(CsvFileWriter writer, PositionChangeMessage data, IMarketDataMetaInfo metaInfo)
		{
			var row = new List<string>();

			row.AddRange(new[]
			{
				data.ServerTime.WriteTimeMls(),
				data.ServerTime.ToString("zzz"),
				data.PortfolioName,
				data.ClientCode,
				data.DepoName,
				data.LimitType.To<string>(),
			});

			foreach (var types in _types)
			{
				row.Add(data.Changes.TryGetValue(types)?.ToString());
			}

			writer.WriteRow(row);

			metaInfo.LastTime = data.ServerTime.UtcDateTime;
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
				LimitType = reader.ReadString().To<TPlusLimits?>(),
			};

			foreach (var type in _types)
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
}