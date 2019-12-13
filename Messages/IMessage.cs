namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// The interface describing an message with <see cref="Clone"/> method.
	/// </summary>
	public interface IMessage
	{
		/// <summary>
		/// Message type.
		/// </summary>
		MessageTypes Type { get; }

		/// <summary>
		/// Local timestamp when a message was received/created.
		/// </summary>
		DateTimeOffset LocalTime { get; set; }

		/// <summary>
		/// Create a copy of <see cref="IMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		IMessage Clone();
	}
}