#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDdeCustomTable.SampleDdeCustomTablePublic
File: QuikCandle.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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