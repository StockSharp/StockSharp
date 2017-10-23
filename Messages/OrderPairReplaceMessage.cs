#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderPairReplaceMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// The message containing the information for modify order's pair.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderPairReplaceMessage : SecurityMessage
	{
		/// <summary>
		/// The message containing the information for modify the first order.
		/// </summary>
		[DataMember]
		public OrderReplaceMessage Message1 { get; set; }

		/// <summary>
		/// The message containing the information for modify the second order.
		/// </summary>
		[DataMember]
		public OrderReplaceMessage Message2 { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderPairReplaceMessage"/>.
		/// </summary>
		public OrderPairReplaceMessage()
			: base(MessageTypes.OrderPairReplace)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="OrderPairReplaceMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderPairReplaceMessage
			{
				LocalTime = LocalTime,
				Message1 = (OrderReplaceMessage)Message1?.Clone(),
				Message2 = (OrderReplaceMessage)Message2?.Clone(),
			};

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + $",{Message1},{Message2}";
		}
	}
}