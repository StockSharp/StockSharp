namespace StockSharp.Messages
{
	using System;

	using Ecng.Interop;
	using Ecng.Localization;

	/// <summary>
	/// Функциональность.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class TargetPlatformAttribute : Attribute
	{
		/// <summary>
		/// Целевая аудитория.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// Платформа.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// Создать <see cref="TargetPlatformAttribute"/>.
		/// </summary>
		/// <param name="preferLanguage">Целевая аудитория.</param>
		/// <param name="platform">Платформа.</param>
		public TargetPlatformAttribute(Languages preferLanguage = Languages.English, Platforms platform = Platforms.AnyCPU)
		{
			PreferLanguage = preferLanguage;
			Platform = platform;
		}
	}
}