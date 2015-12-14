#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: TargetPlatformFeature.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Localization;

	using StockSharp.Localization;

	/// <summary>
	/// Features.
	/// </summary>
	public class TargetPlatformFeature
	{
		/// <summary>
		/// The key for <see cref="LocalizedStrings"/>, by which a localized name will be obtained.
		/// </summary>
		public string LocalizationKey { get; }

		/// <summary>
		/// The target audience.
		/// </summary>
		public Languages PreferLanguage { get; }

		/// <summary>
		/// Platform.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TargetPlatformFeature"/>.
		/// </summary>
		/// <param name="localizationKey">The key for <see cref="LocalizedStrings"/>, by which a localized name will be obtained.</param>
		/// <param name="preferLanguage">The target audience.</param>
		/// <param name="platform">Platform.</param>
		public TargetPlatformFeature(string localizationKey, Languages preferLanguage = Languages.English, Platforms platform = Platforms.AnyCPU)
		{
			if (localizationKey.IsEmpty())
				throw new ArgumentNullException(nameof(localizationKey));

			LocalizationKey = localizationKey;
			PreferLanguage = preferLanguage;
			Platform = platform;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			var str = LocalizedStrings.GetString(LocalizationKey);

			if (PreferLanguage != Languages.English && LocalizedStrings.ActiveLanguage != PreferLanguage)
				str += " ({0})".Put(PreferLanguage.ToString().Substring(0, 2).ToLowerInvariant());

			return str;
		}
	}
}