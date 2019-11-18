namespace StockSharp.Messages
{
	/// <summary>
	/// The interface describing an message with <see cref="Clone"/> method.
	/// </summary>
	public interface IMessage
	{
		/// <summary>
		/// Create a copy of <see cref="IMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		IMessage Clone();
	}
}