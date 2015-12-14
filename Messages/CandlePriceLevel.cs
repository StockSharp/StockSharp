#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: CandlePriceLevel.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// The price level.
	/// </summary>
	[DataContract]
	[Serializable]
	public class CandlePriceLevel : Cloneable<CandlePriceLevel>
	{
		///// <summary>
		///// Initializes a new instance of the <see cref="CandlePriceLevel"/>.
		///// </summary>
		///// <param name="price">Price.</param>
		//public CandlePriceLevel(decimal price)
		//	: this(price, new List<decimal>(), new List<decimal>())
		//{
		//}

		///// <summary>
		///// Initializes a new instance of the <see cref="CandlePriceLevel"/>.
		///// </summary>
		///// <param name="price">Price.</param>
		///// <param name="buyVolumes">The volumes collection to buy.</param>
		///// <param name="sellVolumes">The volumes collection for sale.</param>
		//public CandlePriceLevel(decimal price, List<decimal> buyVolumes, List<decimal> sellVolumes)
		//{
		//	Price = price;

		//	BuyVolume = buyVolumes.Sum();
		//	SellVolume = sellVolumes.Sum();

		//	BuyCount = buyVolumes.Count;
		//	SellCount = sellVolumes.Count;

		//	BuyVolumes = buyVolumes;
		//	SellVolumes = sellVolumes;
		//}

		/// <summary>
		/// Price.
		/// </summary>
		[DataMember]
		public decimal Price { get; set; }

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
		/// Create a copy of <see cref="CandlePriceLevel"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override CandlePriceLevel Clone()
		{
			return new CandlePriceLevel
			{
				Price = Price,
				BuyCount = BuyCount,
				SellCount = SellCount,
				SellVolume = SellVolume,
				BuyVolume = BuyVolume,
				BuyVolumes = BuyVolumes?.ToArray(),
				SellVolumes = SellVolumes?.ToArray(),
			};
		}
	}
}