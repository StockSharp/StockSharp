namespace StockSharp.Messages
{
	using Ecng.Configuration;

	/// <summary>
	/// Services registry.
	/// </summary>
	public static class MessagesServiceRegistry
	{
		/// <summary>
		/// <see cref="ISecurityMessageProvider"/>.
		/// </summary>
		public static ISecurityMessageProvider TrySecurityProvider => ConfigManager.TryGetService<ISecurityMessageProvider>();

		/// <summary>
		/// <see cref="IBoardMessageProvider"/>.
		/// </summary>
		public static IBoardMessageProvider TryBoardProvider => ConfigManager.TryGetService<IBoardMessageProvider>();
	}
}