namespace StockSharp.Messages
{
	using System;

	using Ecng.Interop;
	using Ecng.Localization;

	/// <summary>
	/// Features.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class TargetPlatformAttribute : Attribute
	{
		/// <summary>
		/// The target audience.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// Platform.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TargetPlatformAttribute"/>.
		/// </summary>
		/// <param name="preferLanguage">The target audience.</param>
		/// <param name="platform">Platform.</param>
		public TargetPlatformAttribute(Languages preferLanguage = Languages.English, Platforms platform = Platforms.AnyCPU)
		{
			PreferLanguage = preferLanguage;
			Platform = platform;
		}
	}
}