namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Connect to a server message (uses as a command in outgoing case, event in incoming case).
	/// </summary>
	[DataContract]
	[Serializable]
	public class ConnectMessage : BaseConnectionMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectMessage"/>.
		/// </summary>
		public ConnectMessage()
			: base(MessageTypes.Connect)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="ConnectMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new ConnectMessage
			{
				Error = Error,
				LocalTime = LocalTime,
			};
		}
	}
}