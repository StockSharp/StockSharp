#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: LastMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

	/// <summary>
	/// The message, informing on end of data occurrence.
	/// </summary>
	class LastMessage : Message
	{
		/// <summary>
		/// The data transfer is completed due to error.
		/// </summary>
		public bool IsError { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LastMessage"/>.
		/// </summary>
		public LastMessage()
			: base(ExtendedMessageTypes.Last)
		{
		}

		public override Message Clone()
		{
			return new LastMessage { IsError = IsError };
		}
	}
}