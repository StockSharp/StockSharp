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
	[XamlIcon("Bell")]
	Sound,

	/// <summary>
	/// Popup window.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PopupKey)]
	[XamlIcon("Copy")]
	Popup,

	/// <summary>
	/// Log file.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LogFileKey)]
	[XamlIcon("Logs")]
	Log,

	/// <summary>
	/// Telegram.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TelegramKey)]
	[XamlIcon("Telegram")]
	Telegram,
}