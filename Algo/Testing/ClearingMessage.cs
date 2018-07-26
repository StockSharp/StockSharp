#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: ClearingMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The message about performing clearing on exchange.
	/// </summary>
	public class ClearingMessage : Message
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Shall order book be cleared.
		/// </summary>
		public bool ClearMarketDepth { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ClearingMessage"/>.
		/// </summary>
		public ClearingMessage()
			: base(ExtendedMessageTypes.Clearing)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="ClearingMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new ClearingMessage
			{
				SecurityId = SecurityId,
				ClearMarketDepth = ClearMarketDepth,
			};
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Sec={0}".Put(SecurityId);
		}
	}
}