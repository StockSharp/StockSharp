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
	public class TimeFrameInfoMessage : BaseSubscriptionIdMessage<TimeFrameInfoMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TimeFrameInfoMessage"/>.
		/// </summary>
		public TimeFrameInfoMessage()
			: base(MessageTypes.TimeFrameInfo)
		{
		}

		private TimeSpan[] _timeFrames = ArrayHelper.Empty<TimeSpan>();

		/// <summary>
		/// Possible time-frames.
		/// </summary>
		[DataMember]
		public TimeSpan[] TimeFrames
		{
			get => _timeFrames;
			set => _timeFrames = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.TimeFrames;

		/// <inheritdoc />
		public override void CopyTo(TimeFrameInfoMessage destination)
		{
			base.CopyTo(destination);

			destination.TimeFrames = TimeFrames;
		}

		/// <inheritdoc />
		public override string ToString()
			=> base.ToString() + $",TF={TimeFrames.Select(t => t.ToString()).JoinComma()}";
	}
}