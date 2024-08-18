namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Connection states.
	/// </summary>
	[Serializable]
	[DataContract]
	public enum ConnectionStates
	{
		/// <summary>
		/// Non active.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DisconnectedKey)]
		Disconnected,

		/// <summary>
		/// Disconnect pending.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DisconnectingKey)]
		Disconnecting,

		/// <summary>
		/// Connect pending.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ConnectingKey)]
		Connecting,

		/// <summary>
		/// Connection active.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ConnectedKey)]
		Connected,

		/// <summary>
		/// Error connection.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FailedKey)]
		Failed,
	}
}
