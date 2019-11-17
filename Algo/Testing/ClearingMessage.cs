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
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The message about performing clearing on exchange.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ClearingMessage : Message, ISecurityIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Shall order book be cleared.
		/// </summary>
		[DataMember]
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

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + ",Sec={0}".Put(SecurityId);
		}
	}
}