#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: AggregatedQuote.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Aggregate quote.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class AggregatedQuote : Quote
	{
		private sealed class InnerQuotesList : BaseList<Quote>
		{
			private readonly AggregatedQuote _parent;

			public InnerQuotesList(AggregatedQuote parent)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
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
						throw new ArgumentException(LocalizedStrings.Str421Params.Put(_parent.Price, item.Price), nameof(item));

					if (_parent.OrderDirection != item.OrderDirection)
						throw new ArgumentException(LocalizedStrings.Str422Params.Put(_parent.OrderDirection, item.OrderDirection), nameof(item));

					if (_parent.Security != item.Security)
						throw new ArgumentException(LocalizedStrings.Str423Params.Put(_parent.Security.Id, item.Security.Id), nameof(item));
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
		/// Initializes a new instance of the <see cref="AggregatedQuote"/>.
		/// </summary>
		/// <param name="checkInnerPrice">Whether to check the internal quote price.</param>
		public AggregatedQuote(bool checkInnerPrice = true)
		{
			_checkInnerPrice = checkInnerPrice;
			InnerQuotes = new InnerQuotesList(this);
		}

		/// <summary>
		/// Collection of enclosed quotes, which are combined into a single quote.
		/// </summary>
		public IList<Quote> InnerQuotes { get; }

		/// <summary>
		/// Create a copy of <see cref="AggregatedQuote"/>.
		/// </summary>
		/// <returns>Copy.</returns>
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