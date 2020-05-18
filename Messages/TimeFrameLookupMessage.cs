namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Message to request supported time-frames.
	/// </summary>
	[DataContract]
	[Serializable]
	public class TimeFrameLookupMessage : BaseRequestMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TimeFrameLookupMessage"/>.
		/// </summary>
		public TimeFrameLookupMessage()
			: base(MessageTypes.TimeFrameLookup)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.TimeFrames;

		/// <summary>
		/// Create a copy of <see cref="TimeFrameLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new TimeFrameLookupMessage());
		}
	}
}