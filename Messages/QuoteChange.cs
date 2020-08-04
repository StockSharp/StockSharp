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
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Change actions.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
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
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum QuoteConditions : byte
	{
		/// <summary>
		/// Active.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str238Key)]
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
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.Str273Key)]
	[DescriptionLoc(LocalizedStrings.Str274Key)]
	public struct QuoteChange : IExtendableEntity
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
			_extensionInfo = null;

			_innerQuotes = null;
		}

		/// <summary>
		/// Quote price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str275Key)]
		[MainCategory]
		public decimal Price { get; set; }

		/// <summary>
		/// Quote volume.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str276Key)]
		[MainCategory]
		public decimal Volume { get; set; }

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey, true)]
		[MainCategory]
		public string BoardCode { get; set; }

		[field: NonSerialized]
		private IDictionary<string, object> _extensionInfo;

		/// <inheritdoc />
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		[Obsolete]
		public IDictionary<string, object> ExtensionInfo
		{
			get => _extensionInfo;
			set => _extensionInfo = value;
		}

		/// <summary>
		/// Orders count.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? OrdersCount { get; set; }

		/// <summary>
		/// Start position, related for <see cref="Action"/>.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? StartPosition { get; set; }

		/// <summary>
		/// End position, related for <see cref="Action"/>.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? EndPosition { get; set; }

		/// <summary>
		/// Change action.
		/// </summary>
		[DataMember]
		[Nullable]
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