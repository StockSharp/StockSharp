namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// The interface describing an message with <see cref="Type"/> method.
	/// </summary>
	public interface IMessage : ICloneable
	{
		/// <summary>
		/// Message type.
		/// </summary>
		MessageTypes Type { get; }

		/// <summary>
		/// Local timestamp when a message was received/created.
		/// </summary>
		DateTimeOffset LocalTime { get; set; }
	}
}