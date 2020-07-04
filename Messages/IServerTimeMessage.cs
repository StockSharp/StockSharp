namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// The interface describing an message with <see cref="ServerTime"/> property.
	/// </summary>
	public interface IServerTimeMessage : IMessage
	{
		/// <summary>
		/// Server time.
		/// </summary>
		DateTimeOffset ServerTime { get; set; }
	}
}