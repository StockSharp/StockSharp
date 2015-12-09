namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.IO;
	using System.Text;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The tick serializer in the CSV format.
	/// </summary>
	public class TickCsvSerializer : CsvMarketDataSerializer<ExecutionMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TickCsvSerializer"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="encoding">Encoding.</param>
		public TickCsvSerializer(SecurityId securityId, Encoding encoding = null)
			: base(securityId, encoding)
		{
		}

		/// <summary>
		/// To create empty meta-information.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>Meta-information on data for one day.</returns>
		public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
		{
			return new CsvMetaInfo(date, Encoding, r => r.ReadNullableLong());
		}

		/// <summary>
		/// Write data to the specified writer.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Data.</param>
		protected override void Write(TextWriter writer, ExecutionMessage data)
		{
			writer.Write($"{data.ServerTime.UtcDateTime.ToString(TimeFormat)};{data.ServerTime.ToString("zzz")};{data.TradeId};{data.TradePrice};{data.Volume};{data.OriginSide};{data.OpenInterest};{data.IsSystem}");
		}

		/// <summary>
		/// Load data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="date">Date.</param>
		/// <returns>Data.</returns>
		protected override ExecutionMessage Read(FastCsvReader reader, DateTime date)
		{
			return new ExecutionMessage
			{
				SecurityId = SecurityId,
				ExecutionType = ExecutionTypes.Tick,
				ServerTime = reader.ReadTime(date),
				TradeId = reader.ReadNullableLong(),
				TradePrice = reader.ReadNullableDecimal(),
				Volume = reader.ReadNullableDecimal(),
				OriginSide = reader.ReadNullableEnum<Sides>(),
				OpenInterest = reader.ReadNullableDecimal(),
				IsSystem = reader.ReadNullableBool(),
			};
		}
	}
}