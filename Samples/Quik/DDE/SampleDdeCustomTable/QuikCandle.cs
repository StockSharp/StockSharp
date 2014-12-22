namespace SampleDdeCustomTable
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Quik;

	public class QuikCandleDateTime : Equatable<QuikCandleDateTime>
	{
		[DdeCustomColumn("Дата", Order = 0)]
		public string Date { get; set; }

		[DdeCustomColumn("Время", Order = 1)]
		public string Time { get; set; }

		public override QuikCandleDateTime Clone()
		{
			throw new NotSupportedException();
		}

		public override int GetHashCode()
		{
			return Date.GetHashCode() ^ Time.GetHashCode();
		}

		protected override bool OnEquals(QuikCandleDateTime other)
		{
			return Date == other.Date && Time == other.Time;
		}
	}

	[DdeCustomTable("Исторические свечи")]
	public class QuikCandle
	{
		[Identity]
		[InnerSchema]
		public QuikCandleDateTime DateTime { get; set; }

		[DdeCustomColumn("Цена открытия", Order = 2)]
		public decimal OpenPrice { get; set; }

		[DdeCustomColumn("Максимальная цена", Order = 3)]
		public decimal HighPrice { get; set; }

		[DdeCustomColumn("Минимальная цена", Order = 4)]
		public decimal LowPrice { get; set; }

		[DdeCustomColumn("Цена закрытия", Order = 5)]
		public decimal ClosePrice { get; set; }

		[DdeCustomColumn("Объем", Order = 6)]
		public int Volume { get; set; }
	}
}