#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: StrategyData.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Strategy content types.
	/// </summary>
	[DataContract]
	public enum StrategyContentTypes
	{
		/// <summary>
		/// Source code (if the strategy is distributed in source code).
		/// </summary>
		[EnumMember]
		SourceCode,

		/// <summary>
		/// The compiled build (if the strategy is distributed as a finished build).
		/// </summary>
		[EnumMember]
		CompiledAssembly,

		/// <summary>
		/// Schema in visual designer (if the strategy is distributed as a schema).
		/// </summary>
		[EnumMember]
		Schema,

		/// <summary>
		/// Encrypted version of <see cref="Schema"/>.
		/// </summary>
		[EnumMember]
		EncryptedSchema,

		/// <summary>
		/// The compiled executable (.exe) application.
		/// </summary>
		[EnumMember]
		StandaloneApp,

		/// <summary>
		/// Indicator.
		/// </summary>
		[EnumMember]
		Indicator,
	}

	/// <summary>
	/// Strategy price types.
	/// </summary>
	[DataContract]
	public enum StrategyPriceTypes
	{
		/// <summary>
		/// Lifetime.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LifetimeKey)]
		Lifetime,

		/// <summary>
		/// Per month.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PerMonthKey)]
		PerMonth,

		/// <summary>
		/// Annual.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AnnualKey)]
		Annual
	}

	/// <summary>
	/// The strategy data.
	/// </summary>
	[DataContract]
	public class StrategyData
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// The creation date.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }

		/// <summary>
		/// Name.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Strategy description.
		/// </summary>
		[DataMember]
		public string Description { get; set; }

		/// <summary>
		/// Strategy tags.
		/// </summary>
		[DataMember]
		public string Tags { get; set; }

		///// <summary>
		///// Name (ru).
		///// </summary>
		//[DataMember]
		//public string RuName { get; set; }

		///// <summary>
		///// Strategy description (ru).
		///// </summary>
		//[DataMember]
		//public string RuDescription { get; set; }

		/// <summary>
		/// The identifier of a topic in the forum where the strategy is discussed.
		/// </summary>
		[DataMember]
		public int DescriptionId { get; set; }

		/// <summary>
		/// Type of <see cref="Price"/>.
		/// </summary>
		[DataMember]
		public StrategyPriceTypes PriceType { get; set; }

		/// <summary>
		/// The purchase price.
		/// </summary>
		[DataMember]
		public decimal Price { get; set; }

		///// <summary>
		///// Is the <see cref="Price"/> in USD.
		///// </summary>
		//[DataMember]
		//public bool IsUsd { get; set; }

		/// <summary>
		/// Type of <see cref="Content"/>.
		/// </summary>
		[DataMember]
		public StrategyContentTypes ContentType { get; set; }

		///// <summary>
		///// Content name (file name etc.).
		///// </summary>
		//[DataMember]
		//public string ContentName { get; set; }

		/// <summary>
		/// Content.
		/// </summary>
		[DataMember]
		public long Content { get; set; }

		/// <summary>
		/// The author identifier.
		/// </summary>
		[DataMember]
		public long Author { get; set; }

		/// <summary>
		/// The picture identifier.
		/// </summary>
		[DataMember]
		public long? Picture { get; set; }

		/// <summary>
		/// The content revision.
		/// </summary>
		[DataMember]
		public int Revision { get; set; }

		/// <summary>
		/// User ID.
		/// </summary>
		[DataMember]
		public string UserId { get; set; }

		/// <summary>
		/// Only visible to author.
		/// </summary>
		[DataMember]
		public bool IsPrivate { get; set; }

		/// <summary>
		/// Is colocation available for the strategy.
		/// </summary>
		[DataMember]
		public bool IsColocation { get; set; }

		/// <summary>
		/// Promo price.
		/// </summary>
		[DataMember]
		public decimal? PromoPrice { get; set; }

		/// <summary>
		/// Promo end date.
		/// </summary>
		[DataMember]
		public DateTime? PromoEnd { get; set; }
	}
}