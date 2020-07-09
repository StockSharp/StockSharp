namespace StockSharp.Community
{
	using StockSharp.Messages;

	/// <summary>
	/// Extended <see cref="MessageTypes"/>.
	/// </summary>
	public class CommunityMessageTypes
	{
		/// <summary>
		/// <see cref="FileInfoMessage"/>.
		/// </summary>
		public const MessageTypes FileInfo = (MessageTypes)(-11000);

		/// <summary>
		/// <see cref="ProductInfoMessage"/>.
		/// </summary>
		public const MessageTypes ProductInfo = (MessageTypes)(-11001);

		/// <summary>
		/// <see cref="ProductLookupMessage"/>.
		/// </summary>
		public const MessageTypes ProductLookup = (MessageTypes)(-11002);

		/// <summary>
		/// <see cref="ProductFeedbackMessage"/>.
		/// </summary>
		public const MessageTypes ProductFeedback = (MessageTypes)(-11003);

		internal static DataType ProductInfoType = DataType.Create(typeof(ProductInfoMessage), null).Immutable();
		internal static DataType ProductFeedbackType = DataType.Create(typeof(ProductFeedbackMessage), null).Immutable();
	}
}