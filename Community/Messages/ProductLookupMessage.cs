namespace StockSharp.Community.Messages
{
	using StockSharp.Messages;

	/// <summary>
	/// Product lookup message.
	/// </summary>
	public class ProductLookupMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProductLookupMessage"/>.
		/// </summary>
		public ProductLookupMessage()
			: base(CommunityMessageTypes.ProductLookup)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="ProductFeedbackMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductLookupMessage();
			CopyTo(clone);
			return clone;
		}
	}
}