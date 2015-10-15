namespace StockSharp.Algo
{
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// States of the process.
	/// </summary>
	[DataContract]
	public enum ProcessStates
	{
		/// <summary>
		/// Stopped.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str1113Key)]
		Stopped,

		/// <summary>
		/// Stopping.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str1114Key)]
		Stopping,

		/// <summary>
		/// Started.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str1115Key)]
		Started,
	}
}