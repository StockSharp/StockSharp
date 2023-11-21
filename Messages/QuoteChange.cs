#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: QuoteChange.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Change actions.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum QuoteChangeActions : byte
	{
		/// <summary>
		/// New quote for <see cref="QuoteChange.StartPosition"/>.
		/// </summary>
		[EnumMember]
		New,

		/// <summary>
		/// Update quote for <see cref="QuoteChange.StartPosition"/>.
		/// </summary>
		[EnumMember]
		Update,

		/// <summary>
		/// Delete quotes from <see cref="QuoteChange.StartPosition"/> till <see cref="QuoteChange.EndPosition"/>.
		/// </summary>
		[EnumMember]
		Delete,
	}

	/// <summary>
	/// Quote conditions.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum QuoteConditions : byte
	{
		/// <summary>
		/// Active.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ActiveKey)]
		Active,

		/// <summary>
		/// Indicative.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IndicativeKey)]
		Indicative,
	}

	/// <summary>
	/// Market depth quote representing bid or ask.
	/// </summary>
	[DataContract]
	[Serializable]
	public struct QuoteChange
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QuoteChange"/>.
		/// </summary>
		/// <param name="price">Quote price.</param>
		/// <param name="volume">Quote volume.</param>
		/// <param name="ordersCount">Orders count.</param>
		/// <param name="condition">Quote condition.</param>
		public QuoteChange(decimal price, decimal volume, int? ordersCount = null, QuoteConditions condition = default)
		{
			Price = price;
			Volume = volume;
			OrdersCount = ordersCount;
			Condition = condition;

			StartPosition = null;
			EndPosition = null;
			Action = null;

			BoardCode = null;

			_innerQuotes = null;
		}

		/// <summary>
		/// Quote price.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PriceKey,
			Description = LocalizedStrings.QuotePriceKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public decimal Price { get; set; }

		/// <summary>
		/// Quote volume.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VolumeKey,
			Description = LocalizedStrings.QuoteVolumeKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public decimal Volume { get; set; }

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BoardKey,
			Description = LocalizedStrings.BoardCodeKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string BoardCode { get; set; }

		/// <summary>
		/// Orders count.
		/// </summary>
		[DataMember]
		public int? OrdersCount { get; set; }

		/// <summary>
		/// Start position, related for <see cref="Action"/>.
		/// </summary>
		[DataMember]
		public int? StartPosition { get; set; }

		/// <summary>
		/// End position, related for <see cref="Action"/>.
		/// </summary>
		[DataMember]
		public int? EndPosition { get; set; }

		/// <summary>
		/// Change action.
		/// </summary>
		[DataMember]
		public QuoteChangeActions? Action { get; set; }

		/// <summary>
		/// Quote condition.
		/// </summary>
		[DataMember]
		public QuoteConditions Condition { get; set; }

		private QuoteChange[] _innerQuotes;

		/// <summary>
		/// Collection of enclosed quotes, which are combined into a single quote.
		/// </summary>
		public QuoteChange[] InnerQuotes
		{
			get => _innerQuotes;
			set
			{
				var wasNonNull = _innerQuotes != null;

				_innerQuotes = value;

				if (_innerQuotes is null)
				{
					if (wasNonNull)
					{
						Volume = default;
						OrdersCount = default;
					}
				}
				else
				{
					var volume = 0m;
					var ordersCount = 0;

					foreach (var item in value)
					{
						volume += item.Volume;

						if (item.OrdersCount != null)
							ordersCount += item.OrdersCount.Value;
					}

					Volume = volume;
					OrdersCount = ordersCount.DefaultAsNull();
				}
			}
		}

		/// <inheritdoc />
		public override string ToString() => $"{Price} {Volume}";
	}
}