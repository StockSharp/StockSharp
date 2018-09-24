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

			return asmHolder.GetManifestResourceStream($"{asmHolder.GetName().Name}.{Path.GetFileName("text.csv")}");
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
				LocalizationHelper.DefaultManager.Init(LocalizedStringsExtension.GetResourceStream());
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}
		}

		private static LocalizationManager Manager => LocalizationHelper.DefaultManager;

		/// <summary>
		/// Error handler to track missed translations or resource keys.
		/// </summary>
		public static event Action<string, bool> Missing
		{
			add => Manager.Missing += value;
			remove => Manager.Missing -= value;
		}

		/// <summary>
		/// Current language.
		/// </summary>
		public static Languages ActiveLanguage
		{
			get => Manager.ActiveLanguage;
			set => Manager.ActiveLanguage = value;
		}

		/// <summary>
		/// Get localized string.
		/// </summary>
		/// <param name="resourceId">Resource unique key.</param>
		/// <param name="language">Language.</param>
		/// <returns>Localized string.</returns>
		public static string GetString(string resourceId, Languages? language = null)
		{
			return Manager.GetString(resourceId, language);
		}

		/// <summary>
		/// Web site domain.
		/// </summary>
		public static string Domain => ActiveLanguage == Languages.Russian ? "ru" : "com";
	}
}