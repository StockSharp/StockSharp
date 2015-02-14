namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Агрегированная котировка.
	/// </summary>
	public class AggregatedQuote : Quote
	{
		private sealed class InnerQuotesList : BaseList<Quote>
		{
			private readonly AggregatedQuote _parent;

			public InnerQuotesList(AggregatedQuote parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
			}

			protected override bool OnAdding(Quote item)
			{
				if (Count == 0)
				{
					_parent.Price = item.Price;
					_parent.OrderDirection = item.OrderDirection;
					_parent.Security = item.Security;
				}
				else
				{
					if (_parent._checkInnerPrice && _parent.Price != item.Price)
						throw new ArgumentException(LocalizedStrings.Str421Params.Put(_parent.Price, item.Price), "item");

					if (_parent.OrderDirection != item.OrderDirection)
						throw new ArgumentException(LocalizedStrings.Str422Params.Put(_parent.OrderDirection, item.OrderDirection), "item");

					if (_parent.Security != item.Security)
						throw new ArgumentException(LocalizedStrings.Str423Params.Put(_parent.Security.Id, item.Security.Id), "item");
				}

				_parent.Volume += item.Volume;

				return base.OnAdding(item);
			}

			protected override bool OnClearing()
			{
				_parent.Volume = 0;
				return base.OnClearing();
			}
		}

		private readonly bool _checkInnerPrice;

		/// <summary>
		/// Создать <see cref="AggregatedQuote"/>.
		/// </summary>
		/// <param name="checkInnerPrice">Проверять ли цену внутренней котировки.</param>
		public AggregatedQuote(bool checkInnerPrice = true)
		{
			_checkInnerPrice = checkInnerPrice;
			InnerQuotes = new InnerQuotesList(this);
		}

		/// <summary>
		/// Коллекция вложенных котировок, объединенных в одну единую котировку.
		/// </summary>
		public IList<Quote> InnerQuotes { get; private set; }

		/// <summary>
		/// Создать копию объекта <see cref="AggregatedQuote" />.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Quote Clone()
		{
			var clone = new AggregatedQuote(_checkInnerPrice)
			{
				Security = Security,
				Price = Price,
				OrderDirection = OrderDirection
			};

			clone.InnerQuotes.AddRange(InnerQuotes.Select(q => q.Clone()));
			return clone;
		}
	}
}