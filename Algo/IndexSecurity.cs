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
	using System;
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
		/// Initialize <see cref="IndexSecurity"/>.
		/// </summary>
		protected IndexSecurity()
		{
			Type = SecurityTypes.Index;
			//Board = ExchangeBoard.Associated;
		}

		/// <summary>
		/// To calculate the basket value.
		/// </summary>
		/// <param name="values">Values of basket composite instruments <see cref="BasketSecurity.InnerSecurityIds"/>.</param>
		/// <param name="isPrice">Is price based value calculation.</param>
		/// <returns>The basket value.</returns>
		public decimal Calculate(decimal[] values, bool isPrice)
		{
			var value = OnCalculate(values);

			if (isPrice)
			{
				if (PriceStep != null)
					value = this.ShrinkPrice(value);
			}
			else
			{
				if (VolumeStep != null)
					value = MathHelper.Round(value, VolumeStep.Value, VolumeStep.Value.GetCachedDecimals());
			}

			return value;
		}

		/// <summary>
		/// To calculate the basket value.
		/// </summary>
		/// <param name="values">Values of basket composite instruments <see cref="BasketSecurity.InnerSecurityIds"/>.</param>
		/// <returns>The basket value.</returns>
		protected abstract decimal OnCalculate(decimal[] values);
	}

	/// <summary>
	/// The instruments basket, based on weigh-scales <see cref="Weights"/>.
	/// </summary>
	public class WeightedIndexSecurity : IndexSecurity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WeightedIndexSecurity"/>.
		/// </summary>
		public WeightedIndexSecurity()
		{
			_weights = new CachedSynchronizedDictionary<SecurityId, decimal>();
		}

		private readonly CachedSynchronizedDictionary<SecurityId, decimal> _weights;

		/// <summary>
		/// Instruments and their weighting coefficients in the basket.
		/// </summary>
		public SynchronizedDictionary<SecurityId, decimal> Weights => _weights;

		/// <summary>
		/// Instruments, from which this basket is created.
		/// </summary>
		public override IEnumerable<SecurityId> InnerSecurityIds => _weights.CachedKeys;

		/// <summary>
		/// To calculate the basket value.
		/// </summary>
		/// <param name="values">Values of basket composite instruments <see cref="BasketSecurity.InnerSecurityIds"/>.</param>
		/// <returns>The basket value.</returns>
		protected override decimal OnCalculate(decimal[] values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			if (values.Length != _weights.Count)// || !InnerSecurities.All(prices.ContainsKey))
				throw new ArgumentOutOfRangeException(nameof(values));

			var value = 0M;

			for (var i = 0; i < values.Length; i++)
				value += _weights.CachedValues[i] * values[i];

			return value;
		}

		/// <summary>
		/// Create a copy of <see cref="Security"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Security Clone()
		{
			var clone = new WeightedIndexSecurity();
			clone.Weights.AddRange(_weights.CachedPairs);
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Load security state from <paramref name="text"/>.
		/// </summary>
		/// <param name="text">Value, received from <see cref="ToSerializedString"/>.</param>
		public override void FromSerializedString(string text)
		{
			lock (_weights.SyncRoot)
			{
				_weights.Clear();
				_weights.AddRange(text.Split(",").Select(p =>
				{
					var parts = p.Split("=");
					return new KeyValuePair<SecurityId, decimal>(parts[0].ToSecurityId(), parts[1].To<decimal>());
				}));
			}
		}

		/// <summary>
		/// Save security state to string.
		/// </summary>
		/// <returns>String.</returns>
		public override string ToSerializedString()
		{
			return _weights.CachedPairs.Select(p => $"{p.Key.ToStringId()}={p.Value}").Join(",");
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return _weights.CachedPairs.Select(p => "{0} * {1}".Put(p.Value, p.Key.ToStringId())).Join(", ");
		}
	}
}