namespace StockSharp.Localization
{
	using System;
	using System.Globalization;

	using Ecng.Common;
	using Ecng.Localization;

	/// <summary>
	/// Localized strings.
	/// </summary>
	public static partial class LocalizedStrings
	{
		static LocalizedStrings()
		{
			var activeLang = CultureInfo.CurrentCulture.Name.CompareIgnoreCase("ru-RU")
				? Languages.Russian
				: Languages.English;

			LocalizationHelper.DefaultManager = new LocalizationManager(typeof(LocalizedStrings).Assembly, "text.csv") { ActiveLanguage = activeLang };
		}

		/// <summary>
		/// Error handler to track missed translations or resource keys.
		/// </summary>
		public static event Action<string, bool> Missing
		{
			add { LocalizationHelper.DefaultManager.Missing += value; }
			remove { LocalizationHelper.DefaultManager.Missing -= value; }
		}

		/// <summary>
		/// Current language.
		/// </summary>
		public static Languages ActiveLanguage
		{
			get { return LocalizationHelper.DefaultManager.ActiveLanguage; }
			set { LocalizationHelper.DefaultManager.ActiveLanguage = value; }
		}

		/// <summary>
		/// Get localized string.
		/// </summary>
		/// <param name="resourceId">Resource unique key.</param>
		/// <param name="language">Language.</param>
		/// <returns>Localized string.</returns>
		public static string GetString(string resourceId, Languages? language = null)
		{
			return LocalizationHelper.DefaultManager.GetString(resourceId, language);
		}
	}
}