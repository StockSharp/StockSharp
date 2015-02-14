namespace StockSharp.Algo
{
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Состояния процесса.
	/// </summary>
	[DataContract]
	public enum ProcessStates
	{
		/// <summary>
		/// Остановлено.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str1113Key)]
		Stopped,

		/// <summary>
		/// Останавливается.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str1114Key)]
		Stopping,

		/// <summary>
		/// Запущено.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str1115Key)]
		Started,
	}
}