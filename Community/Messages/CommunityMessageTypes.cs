namespace StockSharp.Community.Messages
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
		/// <see cref="ProductCheckVersionMessage"/>.
		/// </summary>
		public const MessageTypes ProductCheckVersion = (MessageTypes)(-11002);
	}
}