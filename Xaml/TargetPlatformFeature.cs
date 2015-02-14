namespace StockSharp.Xaml
{
	using System;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Localization;

	using StockSharp.Localization;

	/// <summary>
	/// Функциональность.
	/// </summary>
	public class TargetPlatformFeature
	{
		/// <summary>
		/// Ключ для <see cref="LocalizedStrings"/>, по которому будет получено локализованное название.
		/// </summary>
		public string LocalizationKey { get; private set; }

		/// <summary>
		/// Целевая аудитория.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// Платформа.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// Создать <see cref="TargetPlatformFeature"/>.
		/// </summary>
		/// <param name="localizationKey">Ключ для <see cref="LocalizedStrings"/>, по которому будет получено локализованное название.</param>
		/// <param name="preferLanguage">Целевая аудитория.</param>
		/// <param name="platform">Платформа.</param>
		public TargetPlatformFeature(string localizationKey, Languages preferLanguage = Languages.English, Platforms platform = Platforms.AnyCPU)
		{
			if (localizationKey.IsEmpty())
				throw new ArgumentNullException("localizationKey");

			LocalizationKey = localizationKey;
			PreferLanguage = preferLanguage;
			Platform = platform;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		public override string ToString()
		{
			var str = LocalizedStrings.GetString(LocalizationKey);

			if (PreferLanguage != Languages.English && LocalizedStrings.ActiveLanguage != PreferLanguage)
				str += " ({0})".Put(PreferLanguage.ToString().Substring(0, 2).ToLowerInvariant());

			return str;
		}
	}
}