#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: IndexSecurity.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The index, built of instruments. For example, to specify spread at arbitrage or pair trading.
	/// </summary>
	public abstract class IndexSecurity : BasketSecurity
	{
		/// <summary>
		/// Ignore calculation errors.
		/// </summary>
		public bool IgnoreErrors { get; set; }

		/// <summary>
		/// Calculate extended information.
		/// </summary>
		public bool CalculateExtended { get; set; }

		/// <summary>
		/// Fill market-data gaps by zero values.
		/// </summary>
		public bool FillGapsByZeros { get; set; }

		/// <summary>
		/// Initialize <see cref="IndexSecurity"/>.
		/// </summary>
		protected IndexSecurity()
		{
			Type = SecurityTypes.Index;
			//Board = ExchangeBoard.Associated;
		}
	}

	/// <summary>
	/// The instruments basket, based on weigh-scales <see cref="Weights"/>.
	/// </summary>
	[BasketCode("WI")]
	public class WeightedIndexSecurity : IndexSecurity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WeightedIndexSecurity"/>.
		/// </summary>
		public WeightedIndexSecurity()
		{
			Weights = new CachedSynchronizedDictionary<SecurityId, decimal>();
		}

		/// <summary>
		/// Instruments and their weighting coefficients in the basket.
		/// </summary>
		public CachedSynchronizedDictionary<SecurityId, decimal> Weights { get; }

		/// <inheritdoc />
		public override IEnumerable<SecurityId> InnerSecurityIds => Weights.CachedKeys;

		/// <inheritdoc />
		public override Security Clone()
		{
			var clone = new WeightedIndexSecurity();
			clone.Weights.AddRange(Weights.CachedPairs);
			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		protected override void FromSerializedString(string text)
		{
			lock (Weights.SyncRoot)
			{
				Weights.Clear();
				Weights.AddRange(text.Split(",").Select(p =>
				{
					var parts = p.Split("=");
					return new KeyValuePair<SecurityId, decimal>(parts[0].ToSecurityId(), parts[1].To<decimal>());
				}));
			}
		}

		/// <inheritdoc />
		protected override string ToSerializedString()
		{
			return Weights.CachedPairs.Select(p => $"{p.Key.ToStringId()}={p.Value}").Join(",");
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return Weights.CachedPairs.Select(p => $"{p.Value} * {p.Key.ToStringId()}").Join(", ");
		}
	}
}