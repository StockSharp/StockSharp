namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// Error message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ErrorMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorMessage"/>.
		/// </summary>
		public ErrorMessage()
			: base(MessageTypes.Error)
		{
		}

		/// <summary>
		/// Error info.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Error={Message}".PutEx(Error);
		}

		/// <summary>
		/// Create a copy of <see cref="ErrorMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new ErrorMessage
			{
				Error = Error,
				LocalTime = LocalTime,
			};
		}
	}
}