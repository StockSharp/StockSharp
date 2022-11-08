namespace StockSharp.Messages
{
	using System;

	using StockSharp.Localization;

	/// <summary>
	/// Change time interval updates message.
	/// </summary>
	public class ChangeTimeIntervalMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChangeTimeIntervalMessage"/>.
		/// </summary>
		public ChangeTimeIntervalMessage()
			: base(MessageTypes.ChangeTimeInterval)
		{
		}

		private TimeSpan _interval;

		/// <summary>
		/// Interval.
		/// </summary>
		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_interval = value;
			}
		}

		/// <summary>
		/// Create a copy of <see cref="ChangeTimeIntervalMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new ChangeTimeIntervalMessage
			{
				Interval = Interval,
			};
		}
	}
}