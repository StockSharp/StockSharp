namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Product lookup message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ProductPermissionLookupMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProductPermissionLookupMessage"/>.
		/// </summary>
		public ProductPermissionLookupMessage()
			: base(CommunityMessageTypes.ProductPermissionLookup)
		{
		}

		/// <summary>
		/// Product.
		/// </summary>
		[DataMember]
		public long ProductId { get; set; }

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.ProductPermissionType;

		/// <summary>
		/// Create a copy of <see cref="ProductPermissionLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductPermissionLookupMessage
			{
				ProductId = ProductId,
			};
			CopyTo(clone);
			return clone;
		}
	}
}