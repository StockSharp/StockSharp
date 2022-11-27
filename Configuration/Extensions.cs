#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Configuration.ConfigurationPublic
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Configuration
{
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Configuration;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Instance of the root section <see cref="StockSharpSection"/>.
		/// </summary>
		public static StockSharpSection RootSection => ConfigManager.InnerConfig.Sections.OfType<StockSharpSection>().FirstOrDefault();

		/// <summary>
		/// </summary>
		public static long? TryGetProductId()
			=> Assembly
				.GetEntryAssembly()?
				.GetAttribute<ProductIdAttribute>()?
				.ProductId;
	}
}