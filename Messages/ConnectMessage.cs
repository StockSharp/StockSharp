#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: ConnectMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Connect to a server message (uses as a command in outgoing case, event in incoming case).
	/// </summary>
	[DataContract]
	[Serializable]
	public class ConnectMessage : BaseConnectionMessage, IServerTimeMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectMessage"/>.
		/// </summary>
		public ConnectMessage()
			: base(MessageTypes.Connect)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="ConnectMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ConnectMessage();
			CopyTo(clone);
			return clone;
		}

		DateTimeOffset IServerTimeMessage.ServerTime
		{
			get => LocalTime;
			set => LocalTime = value;
		}
	}
}