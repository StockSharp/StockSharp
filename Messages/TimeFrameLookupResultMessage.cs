namespace StockSharp.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// Time-frames search result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class TimeFrameLookupResultMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TimeFrameLookupResultMessage"/>.
		/// </summary>
		public TimeFrameLookupResultMessage()
			: base(MessageTypes.TimeFrameLookupResult)
		{
		}

		/// <summary>
		/// ID of the original message <see cref="TimeFrameLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		private TimeSpan[] _timeFrames = ArrayHelper.Empty<TimeSpan>();

		/// <summary>
		/// Available timeframes of historical data.
		/// </summary>
		[DataMember]
		public TimeSpan[] TimeFrames
		{
			get => _timeFrames;
			set => _timeFrames = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Lookup error info.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Create a copy of <see cref="TimeFrameLookupResultMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new TimeFrameLookupResultMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected TimeFrameLookupResultMessage CopyTo(TimeFrameLookupResultMessage destination)
		{
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.Error = Error;
			destination.TimeFrames = TimeFrames.ToArray();

			this.CopyExtensionInfo(destination);

			return destination;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Orig={OriginalTransactionId}";

			if (Error != null)
				str += $",Error={Error.Message}";
			else
				str += $",TF={TimeFrames.Select(t => t.ToString()).Join(",")}";

			return str;
		}
	}
}