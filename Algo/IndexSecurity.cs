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
		/// <param name="prices">Prices of basket composite instruments <see cref="BasketSecurity.InnerSecurities"/>.</param>
		/// <returns>The basket value.</returns>
		public abstract decimal Calculate(decimal[] prices);
	}

	/// <summary>
	/// The instruments basket, based on weigh-scales <see cref="WeightedIndexSecurity.Weights"/>.
	/// </summary>
	public class WeightedIndexSecurity : IndexSecurity
	{
		private sealed class WeightsDictionary : CachedSynchronizedDictionary<Security, decimal>
		{
			private readonly WeightedIndexSecurity _parent;

			public WeightsDictionary(WeightedIndexSecurity parent)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;
			}

			public override void Add(Security key, decimal value)
			{
				base.Add(key, value);
				RefreshName();
			}

			public override bool Remove(Security key)
			{
				if (base.Remove(key))
				{
					RefreshName();
					return true;
				}

				return false;
			}

			public override void Clear()
			{
				base.Clear();
				RefreshName();
			}

			private void RefreshName()
			{
				_parent.Id = GetName(s => s.Id);
				_parent.Code = GetName(s => s.Code);
				_parent.Name = GetName(s => s.Name);
			}

			private string GetName(Func<Security, string> getSecurityName)
			{
				return this.Select(p => "{0} * {1}".Put(p.Value, getSecurityName(p.Key))).Join(", ");
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WeightedIndexSecurity"/>.
		/// </summary>
		public WeightedIndexSecurity()
		{
			_weights = new WeightsDictionary(this);
		}

		private readonly WeightsDictionary _weights;

		/// <summary>
		/// Instruments and their weighting coefficients in the basket.
		/// </summary>
		public SynchronizedDictionary<Security, decimal> Weights => _weights;

		/// <summary>
		/// Instruments, from which this basket is created.
		/// </summary>
		public override IEnumerable<Security> InnerSecurities => _weights.CachedKeys;

		/// <summary>
		/// To calculate the basket value.
		/// </summary>
		/// <param name="prices">Prices of basket composite instruments <see cref="BasketSecurity.InnerSecurities"/>.</param>
		/// <returns>The basket value.</returns>
		public override decimal Calculate(decimal[] prices)
		{
			if (prices == null)
				throw new ArgumentNullException(nameof(prices));

			if (prices.Length != _weights.Count)// || !InnerSecurities.All(prices.ContainsKey))
				throw new ArgumentOutOfRangeException(nameof(prices));

			decimal retVal = 0;

			for (var i = 0; i < prices.Length; i++)
				retVal += _weights.CachedValues[i] * prices[i];

			return retVal;
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
	}
}