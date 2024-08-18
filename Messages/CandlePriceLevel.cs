namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

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
}