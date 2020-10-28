namespace StockSharp.Community
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Product content types.
	/// </summary>
	[DataContract]
	public enum ProductContentTypes
	{
		/// <summary>
		/// Source code (if the strategy is distributed in source code).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str3180Key)]
		SourceCode,

		/// <summary>
		/// The compiled build (if the strategy is distributed as a finished build).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str3182Key)]
		CompiledAssembly,

		/// <summary>
		/// Schema in visual designer (if the strategy is distributed as a schema).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SchemaKey)]
		Schema,

		/// <summary>
		/// Encrypted version of <see cref="Schema"/>.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EncryptedSchemaKey)]
		EncryptedSchema,

		/// <summary>
		/// The compiled executable (.exe) application.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StandaloneAppKey)]
		StandaloneApp,

		/// <summary>
		/// Indicator.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1981Key)]
		Indicator,

		/// <summary>
		/// Crypto connector.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CryptoKey)]
		CryptoConnector,

		/// <summary>
		/// Stock connector.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StockKey)]
		StockConnector,

		/// <summary>
		/// The diagram element.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DiagramElementKey)]
		DiagramElement,
	}

	/// <summary>
	/// Product content types.
	/// </summary>
	[DataContract]
	[Flags]
	public enum ProductContentTypes2 : long
	{
		/// <summary>
		/// Source code (if the strategy is distributed in source code).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str3180Key)]
		SourceCode = 1,

		/// <summary>
		/// The compiled build (if the strategy is distributed as a finished build).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str3182Key)]
		CompiledAssembly = SourceCode << 1,

		/// <summary>
		/// Schema in visual designer (if the strategy is distributed as a schema).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SchemaKey)]
		Schema = CompiledAssembly << 1,

		/// <summary>
		/// Encrypted version of <see cref="Schema"/>.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EncryptedSchemaKey)]
		EncryptedSchema = Schema << 1,

		/// <summary>
		/// The compiled executable (.exe) application.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StandaloneAppKey)]
		StandaloneApp = EncryptedSchema << 1,

		/// <summary>
		/// Indicator.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1981Key)]
		Indicator = StandaloneApp << 1,

		/// <summary>
		/// Crypto connector.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CryptoKey)]
		CryptoConnector = Indicator << 1,

		/// <summary>
		/// Stock connector.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StockKey)]
		StockConnector = CryptoConnector << 1,

		/// <summary>
		/// The diagram element.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DiagramElementKey)]
		DiagramElement = StockConnector << 1,
	}
}