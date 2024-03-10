namespace StockSharp.Alerts;

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
	[Obsolete]
	Sms,

	/// <summary>
	/// Email.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EmailKey)]
	[Obsolete]
	Email,

	/// <summary>
	/// Log file.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LogFileKey)]
	Log,

	/// <summary>
	/// Telegram.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TelegramKey)]
	Telegram,
}