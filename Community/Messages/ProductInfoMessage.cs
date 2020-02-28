namespace StockSharp.Community.Messages
{
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Product info message.
	/// </summary>
	public class ProductInfoMessage : Message, IOriginalTransactionIdMessage
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Name.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductInfoMessage"/>.
		/// </summary>
		public ProductInfoMessage()
			: base(CommunityMessageTypes.ProductInfo)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="ProductInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductInfoMessage
			{
				Id = Id,
				Name = Name,
				OriginalTransactionId = OriginalTransactionId,
			};
			CopyTo(clone);
			return clone;
		}
	}
}