namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Product category message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ProductCategoryMessage : BaseSubscriptionIdMessage<ProductCategoryMessage>
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

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductCategoryMessage"/>.
		/// </summary>
		public ProductCategoryMessage()
			: base(CommunityMessageTypes.ProductCategory)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.ProductCategoryType;

		/// <summary>
		/// Create a copy of <see cref="ProductCategoryMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductCategoryMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public override void CopyTo(ProductCategoryMessage destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			base.CopyTo(destination);

			destination.Id = Id;
			destination.Name = Name;
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",Id={Id},Name={Name}";
	}
}