namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	internal class PositionsResponse : BaseResponse
	{
		public IEnumerable<MoneyPosition> MoneyPositions { get; set; }
		public IEnumerable<SecPosition> SecPositions { get; set; }
		public IEnumerable<FortsPosition> FortsPositions { get; set; }
		public IEnumerable<FortsMoney> FortsMoneys { get; set; }
		public IEnumerable<FortsCollaterals> FortsCollateralses { get; set; }
		public IEnumerable<SpotLimit> SpotLimits { get; set; }
	}

	internal class FortsPosition : Position
	{
		public IEnumerable<Market> Markets { get; internal set; }
		public int StartNet { get; set; }
		public int OpenBuys { get; set; }
		public int OpenSells { get; set; }
		public int TotalNet { get; set; }
		public int TodayBuy { get; set; }
		public int TodaySell { get; set; }
		public decimal OptMargin { get; set; }
		public decimal VarMargin { get; set; }
		public long ExpirationPos { get; set; }
		public decimal? UsedSellSpotLimit { get; set; }
		public decimal? SellSpotLimit { get; set; }
		public decimal? Netto { get; set; }
		public decimal? Kgo { get; set; }
	}

	internal class SpotLimit : Position
	{
		public IEnumerable<Market> Markets { get; internal set; }
		public decimal BuyLimit { get; set; }
		public decimal BuyLimitUsed { get; set; }
	}

	internal class FortsCollaterals : Position
	{
		public IEnumerable<Market> Markets { get; internal set; }
		public decimal Current { get; set; }
		public decimal Blocked { get; set; }
		public decimal Free { get; set; }
	}

	internal class FortsMoney : FortsCollaterals
	{
		public decimal VarMargin { get; set; }
	}

	internal class SecPosition : Position
	{
		public string Register { get; set; }
		public decimal SaldoIn { get; set; }
		public decimal SaldoMin { get; set; }
		public decimal Bought { get; set; }
		public decimal Sold { get; set; }
		public decimal Saldo { get; set; }
		public decimal OrdBuy { get; set; }
		public decimal OrdSell { get; set; }
	}

	internal class MoneyPosition : Position
	{
		public IEnumerable<Market> Markets { get; internal set; }
		public string Register { get; set; }
		public string Asset { get; set; }
		public decimal SaldoIn { get; set; }
		public decimal Bought { get; set; }
		public decimal Sold { get; set; }
		public decimal Saldo { get; set; }
		public decimal OrdBuy { get; set; }
		public decimal OrdBuyCond { get; set; }
		public decimal Commission { get; set; }
	}

	internal class Position
	{
		public string Client { get; set; }
		public int SecId { get; set; }
		public int Market { get; set; }
		public string SecCode { get; set; }
		public string ShortName { get; set; }
	}
}