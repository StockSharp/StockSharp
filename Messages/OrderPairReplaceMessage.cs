namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

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
				Message1 = Message1.CloneNullable(),
				Message2 = Message2.CloneNullable(),
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
			return base.ToString() + ",{0},{1}".Put(Message1, Message2);
		}
	}
}