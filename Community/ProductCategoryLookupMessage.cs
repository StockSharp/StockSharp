namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Product category lookup message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ProductCategoryLookupMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProductCategoryLookupMessage"/>.
		/// </summary>
		public ProductCategoryLookupMessage()
			: base(CommunityMessageTypes.ProductFeedbackLookup)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.ProductCategoryType;

		/// <summary>
		/// Create a copy of <see cref="ProductCategoryLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductCategoryLookupMessage();

			CopyTo(clone);
			return clone;
		}
	}
}