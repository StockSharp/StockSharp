namespace StockSharp.Logging
{
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Уровни лог-сообщений <see cref="LogMessage"/>.
	/// </summary>
	[DataContract]
	public enum LogLevels
	{
		/// <summary>
		/// Использовать уровень логирования контейнера.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.InheritedKey)]
		[EnumMember]
		Inherit,

		/// <summary>
		/// Отладочное сообщение, информация, предупреждения и ошибки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str12Key)]
		Debug,
		
		/// <summary>
		/// Информация, предупреждения и ошибки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.InfoKey)]
		Info,

		/// <summary>
		/// Предупреждения и ошибки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.WarningsKey)]
		Warning,
		
		/// <summary>
		/// Только ошибки.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ErrorsKey)]
		Error,

		/// <summary>
		/// Логи выключены.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.OffKey)]
		Off,
	}
}