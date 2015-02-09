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
	/// Индекс, построенный из инструментов. Например, для задание спреда при арбитраже или парном трейдинге.
	/// </summary>
	public abstract class IndexSecurity : BasketSecurity
	{
		/// <summary>
		/// Инициализировать <see cref="IndexSecurity"/>.
		/// </summary>
		protected IndexSecurity()
		{
			Type = SecurityTypes.Index;
		}

		/// <summary>
		/// Вычислить значение корзины.
		/// </summary>
		/// <param name="prices">Цены составных инструментов корзины <see cref="BasketSecurity.InnerSecurities"/>.</param>
		/// <returns>Значение корзины.</returns>
		public abstract decimal? Calculate(IDictionary<Security, decimal> prices);
	}

	/// <summary>
	/// Корзина инструментов, основанная на весах <see cref="Weights"/>.
	/// </summary>
	public class WeightedIndexSecurity : IndexSecurity
	{
		private sealed class WeightsDictionary : CachedSynchronizedDictionary<Security, decimal>
		{
			private readonly WeightedIndexSecurity _parent;

			public WeightsDictionary(WeightedIndexSecurity parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

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
		/// Создать <see cref="WeightedIndexSecurity"/>.
		/// </summary>
		public WeightedIndexSecurity()
		{
			_weights = new WeightsDictionary(this);
		}

		private readonly WeightsDictionary _weights;

		/// <summary>
		/// Инструменты и их весовые коэффициенты в корзине.
		/// </summary>
		public SynchronizedDictionary<Security, decimal> Weights
		{
			get { return _weights; }
		}

		/// <summary>
		/// Инструменты, из которых создана данная корзина.
		/// </summary>
		public override IEnumerable<Security> InnerSecurities
		{
			get { return _weights.CachedKeys; }
		}

		/// <summary>
		/// Вычислить значение корзины.
		/// </summary>
		/// <param name="prices">Цены составных инструментов корзины <see cref="BasketSecurity.InnerSecurities"/>.</param>
		/// <returns>Значение корзины.</returns>
		public override decimal? Calculate(IDictionary<Security, decimal> prices)
		{
			if (prices == null)
				throw new ArgumentNullException("prices");

			if (prices.Count != _weights.Count || !InnerSecurities.All(prices.ContainsKey))
				return null;

			return prices.Sum(pair => _weights[pair.Key] * pair.Value);
		}

		/// <summary>
		/// Создать копию объекта <see cref="Security"/>.
		/// </summary>
		/// <returns>Копия объекта.</returns>
		public override Security Clone()
		{
			var clone = new WeightedIndexSecurity();
			clone.Weights.AddRange(Weights.SyncGet(d => d.ToArray()));
			CopyTo(clone);
			return clone;
		}
	}
}