#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: ResetMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// Reset state message.
	/// </summary>
	public sealed class ResetMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ResetMessage"/>.
		/// </summary>
		public ResetMessage()
			: base(MessageTypes.Reset)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="ResetMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			throw new NotSupportedException();
		}
	}
}