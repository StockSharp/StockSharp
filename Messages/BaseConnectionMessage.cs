namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// Base connect/disconnect message.
	/// </summary>
	[DataContract]
	[Serializable]
	public abstract class BaseConnectionMessage : Message
	{
		/// <summary>
		/// Initialize <see cref="BaseConnectionMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected BaseConnectionMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Information about the error connection or disconnection.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + (Error == null ? null : ",Error={Message}".PutEx(Error));
		}
	}
}