namespace StockSharp.Alerts
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Alert types.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum AlertNotifications
	{
		/// <summary>
		/// Sound.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SoundKey)]
		Sound,

		/// <summary>
		/// Speech.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SpeechKey)]
		[Obsolete]
		Speech,

		/// <summary>
		/// Popup window.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PopupKey)]
		Popup,

		/// <summary>
		/// SMS.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SmsKey)]
		Sms,

		/// <summary>
		/// Email.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EmailKey)]
		Email,

		/// <summary>
		/// Log file.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LogFileKey)]
		Log,
	}
}