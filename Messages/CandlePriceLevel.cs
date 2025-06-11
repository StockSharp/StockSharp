namespace StockSharp.Messages;

/// <summary>
/// The price level.
/// </summary>
[DataContract]
[Serializable]
public struct CandlePriceLevel// : ICloneable<CandlePriceLevel>
{
	/// <summary>
	/// Price.
	/// </summary>
	[DataMember]
	public decimal Price { get; set; }

	/// <summary>
	/// The volume of bids and asks.
	/// </summary>
	[DataMember]
	public decimal TotalVolume { get; set; }

	/// <summary>
	/// The volume of bids.
	/// </summary>
	[DataMember]
	public decimal BuyVolume { get; set; }

	/// <summary>
	/// The volume of asks.
	/// </summary>
	[DataMember]
	public decimal SellVolume { get; set; }

	/// <summary>
	/// The number of bids.
	/// </summary>
	[DataMember]
	public int BuyCount { get; set; }

	/// <summary>
	/// The number of asks.
	/// </summary>
	[DataMember]
	public int SellCount { get; set; }

	/// <summary>
	/// The volumes collection of bids.
	/// </summary>
	[DataMember]
	public IEnumerable<decimal> BuyVolumes { get; set; }

	/// <summary>
	/// The volumes collection of asks.
	/// </summary>
	[DataMember]
	public IEnumerable<decimal> SellVolumes { get; set; }

	/// <summary>
	/// Join two <see cref="CandlePriceLevel"/>.
	/// </summary>
	/// <param name="other">Second part.</param>
	/// <returns>Joined <see cref="CandlePriceLevel"/>.</returns>
	public CandlePriceLevel Join(CandlePriceLevel other)
	{
		var totalVol = other.TotalVolume;

		if (totalVol == 0)
			totalVol = other.BuyVolume + other.SellVolume;

		return new()
		{
			Price = Price,
			TotalVolume = TotalVolume + totalVol,
			BuyVolume = BuyVolume + other.BuyVolume,
			SellVolume = SellVolume + other.SellVolume,
			BuyCount = BuyCount + other.BuyCount,
			SellCount = SellCount + other.SellCount,
			BuyVolumes = BuyVolumes?.Concat(other.BuyVolumes ?? []) ?? other.BuyVolumes,
			SellVolumes = SellVolumes?.Concat(other.SellVolumes ?? []) ?? other.SellVolumes,
		};
	}

	/// <inheritdoc />
	public override readonly string ToString()
	{
		var str = $"P:{Price} V:{TotalVolume}";

		if (BuyVolume != 0 || SellVolume != 0)
			str += $" {BuyVolume}/{SellVolume} CNT:{BuyCount}/{SellCount}";

		return str;
	}

	///// <summary>
	///// Create a copy of <see cref="CandlePriceLevel"/>.
	///// </summary>
	///// <returns>Copy.</returns>
	//public CandlePriceLevel Clone()
	//{
	//	return new CandlePriceLevel
	//	{
	//		Price = Price,
	//		BuyCount = BuyCount,
	//		SellCount = SellCount,
	//		SellVolume = SellVolume,
	//		BuyVolume = BuyVolume,
	//		TotalVolume = TotalVolume,
	//		BuyVolumes = BuyVolumes?.ToArray(),
	//		SellVolumes = SellVolumes?.ToArray(),
	//	};
	//}

	//object ICloneable.Clone()
	//{
	//	return Clone();
	//}
}