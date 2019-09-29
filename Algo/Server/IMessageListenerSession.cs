namespace StockSharp.Algo.Server
{
	/// <summary>
	/// The interface describing a session, create by <see cref="IMessageListener"/>.
	/// </summary>
	public interface IMessageListenerSession
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		string Id { get; }
	}
}