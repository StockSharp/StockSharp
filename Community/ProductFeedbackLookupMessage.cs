namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Product feedback lookup message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ProductFeedbackLookupMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProductFeedbackLookupMessage"/>.
		/// </summary>
		public ProductFeedbackLookupMessage()
			: base(CommunityMessageTypes.ProductFeedbackLookup)
		{
		}

		/// <summary>
		/// Product.
		/// </summary>
		[DataMember]
		public long ProductId { get; set; }

		/// <summary>
		/// Own.
		/// </summary>
		[DataMember]
		public bool Own { get; set; }

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.ProductFeedbackType;

		/// <summary>
		/// Create a copy of <see cref="ProductFeedbackMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductFeedbackLookupMessage
			{
				ProductId = ProductId,
				Own = Own,
			};

			CopyTo(clone);
			return clone;
		}
	}
}