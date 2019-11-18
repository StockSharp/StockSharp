namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// The interface describing an message with <see cref="Error"/> property.
	/// </summary>
	public interface IErrorMessage : IMessage
	{
		/// <summary>
		/// Error info.
		/// </summary>
		Exception Error { get; set; }
	}
}