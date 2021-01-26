#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Localization.Localization
File: LocalizedStrings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Localization
{
	using System;
	using System.Diagnostics;
	using System.IO;

	using Ecng.Localization;
	using Ecng.Configuration;

	/// <summary>
	/// Extension for <see cref="LocalizedStrings"/>.
	/// </summary>
	public static class LocalizedStringsExtension
	{
		/// <summary>
		/// 
		/// </summary>
		public static Func<Stream> GetResourceStream = () =>
		{
			var asmHolder = typeof(LocalizedStrings).Assembly;

			return asmHolder.GetManifestResourceStream($"{asmHolder.GetName().Name}.{Path.GetFileName("translation.json")}");
		};
	}

	/// <summary>
	/// Localized strings.
	/// </summary>
	public static partial class LocalizedStrings
	{
		static LocalizedStrings()
		{
			try
			{
				var manager = new LocalizationManager();
				manager.Init(new StreamReader(LocalizedStringsExtension.GetResourceStream()));
				ConfigManager.RegisterService(manager);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}
		}

		/// <summary>
		/// Localization manager.
		/// </summary>
		public static LocalizationManager LocalizationManager => ConfigManager.TryGetService<LocalizationManager>();

		/// <summary>
		/// Error handler to track missed translations or resource keys.
		/// </summary>
		public static event Action<string, bool> Missing
		{
			add => LocalizationManager.Missing += value;
			remove => LocalizationManager.Missing -= value;
		}

		/// <summary>
		/// Current language.
		/// </summary>
		public static string ActiveLanguage
		{
			get => LocalizationManager.ActiveLanguage;
			set => LocalizationManager.ActiveLanguage = value;
		}

		/// <summary>
		/// Get localized string.
		/// </summary>
		/// <param name="resourceId">Resource unique key.</param>
		/// <param name="language">Language.</param>
		/// <returns>Localized string.</returns>
		public static string GetString(string resourceId, string language = null)
		{
			return LocalizationManager.GetString(resourceId, language);
		}

		/// <summary>
		/// Web site domain.
		/// </summary>
		public static string Domain => ActiveLanguage == LangCodes.Ru ? "ru" : "com";
	}
}