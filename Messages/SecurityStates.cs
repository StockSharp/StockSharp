namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Состояния инструмента.
	/// </summary>
	[Serializable]
	[DataContract]
	public enum SecurityStates
	{
		/// <summary>
		/// Торгуется.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.SecurityActiveKey)]
		Trading,

		/// <summary>
		/// Торги приостановлены.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.SecuritySuspendedKey)]
		Stoped,
	}
}