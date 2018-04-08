#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: ConnectionStates.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
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
		[EnumDisplayNameLoc(LocalizedStrings.DisconnectedKey)]
		Disconnected,

		/// <summary>
		/// Disconnect pending.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.DisconnectingKey)]
		Disconnecting,

		/// <summary>
		/// Connect pending.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ConnectingKey)]
		Connecting,

		/// <summary>
		/// Connection active.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ConnectedKey)]
		Connected,

		/// <summary>
		/// Error connection.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.FailedKey)]
		Failed,
	}
}
