#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: TimeInForce.cs
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
	/// Limit order time in force.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum TimeInForce
	{
		/// <summary>
		/// Put in queue.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str405Key)]
		PutInQueue,

		/// <summary>
		/// Fill Or Kill.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.FOKKey)]
		MatchOrCancel,

		/// <summary>
		/// Immediate Or Cancel.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.IOCKey)]
		CancelBalance,
	}
}