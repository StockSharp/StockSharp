namespace StockSharp.Messages
{
	/// <summary>
	/// Connection states.
	/// </summary>
	public enum ConnectionStates
	{
		/// <summary>
		/// Non active.
		/// </summary>
		Disconnected,

		/// <summary>
		/// Disconnect pending.
		/// </summary>
		Disconnecting,

		/// <summary>
		/// Connect pending.
		/// </summary>
		Connecting,

		/// <summary>
		/// Connection active.
		/// </summary>
		Connected,

		/// <summary>
		/// Error connection.
		/// </summary>
		Failed,
	}
}
