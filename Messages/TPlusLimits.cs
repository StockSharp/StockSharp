#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: TPlusLimits.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.ComponentModel;

	/// <summary>
	/// Т+ limit types.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum TPlusLimits
	{
		/// <summary>
		/// Т+0.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("T+0")]
		T0,

		/// <summary>
		/// Т+1.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("T+1")]
		T1,

		/// <summary>
		/// Т+2.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("T+2")]
		T2,
		
		/// <summary>
		/// Т+x.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("T+x")]
		Tx,
	}
}