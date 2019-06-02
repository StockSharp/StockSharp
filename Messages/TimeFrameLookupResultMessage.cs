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
	public class TimeFrameLookupResultMessage : BaseResultMessage<TimeFrameLookupResultMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TimeFrameLookupResultMessage"/>.
		/// </summary>
		public TimeFrameLookupResultMessage()
			: base(MessageTypes.TimeFrameLookupResult)
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
		protected override void CopyTo(TimeFrameLookupResultMessage destination)
		{
			base.CopyTo(destination);
			destination.TimeFrames = TimeFrames.ToArray();
		}

		/// <inheritdoc />
		public override string ToString()
			=> base.ToString() + $",TF={TimeFrames.Select(t => t.ToString()).Join(",")}";
	}
}