namespace StockSharp.Messages
{
	using System;

	using Ecng.Serialization;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Connection tracking settings <see cref="IMessageAdapter"/> with a server.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str977Key)]
	[DescriptionLoc(LocalizedStrings.Str978Key)]
	[ExpandableObject]
	public class ReConnectionSettings : IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReConnectionSettings"/>.
		/// </summary>
		public ReConnectionSettings()
		{
		}

		private TimeSpan _interval = TimeSpan.FromSeconds(10);

		/// <summary>
		/// The interval at which attempts will establish a connection. The default value is 10 seconds.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str175Key)]
		[DescriptionLoc(LocalizedStrings.Str176Key)]
		public TimeSpan Interval
		{
			get { return _interval; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str177);

				_interval = value;
			}
		}

		private int _attemptCount;

		/// <summary>
		/// The number of attempts to establish the initial connection, if it has not been established (timeout, network failure, etc.). The default value is 0. To establish infinite number uses -1.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str178Key)]
		[DescriptionLoc(LocalizedStrings.Str179Key)]
		public int AttemptCount
		{
			get { return _attemptCount; }
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str177);

				_attemptCount = value;
			}
		}

		private int _reAttemptCount = 100;

		/// <summary>
		/// The number of attempts to reconnect if the connection was lost during the operation. The default value is 100. To establish infinite number uses -1.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str180Key)]
		[DescriptionLoc(LocalizedStrings.Str181Key)]
		public int ReAttemptCount
		{
			get { return _reAttemptCount; }
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str177);

				_reAttemptCount = value;
			}
		}

		private TimeSpan _timeOutInterval = TimeSpan.FromSeconds(30);

		/// <summary>
		/// Timeout successful connection / disconnection. If the value is <see cref="TimeSpan.Zero"/>, the monitoring is performed. The default value is 30 seconds.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str182Key)]
		[DescriptionLoc(LocalizedStrings.Str183Key)]
		public TimeSpan TimeOutInterval
		{
			get { return _timeOutInterval; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str177);

				_timeOutInterval = value;
			}
		}

		private WorkingTime _workingTime = new WorkingTime();

		/// <summary>
		/// Schedule, during which it is necessary to make the connection. For example, there is no need to track connection when trading on the exchange finished.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str184Key)]
		[DescriptionLoc(LocalizedStrings.Str185Key)]
		public WorkingTime WorkingTime
		{
			get { return _workingTime; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_workingTime = value;
			}
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			if (storage.ContainsKey("WorkingTime"))
				WorkingTime.Load(storage.GetValue<SettingsStorage>("WorkingTime"));

			Interval = storage.GetValue<TimeSpan>("Interval");
			AttemptCount = storage.GetValue<int>("AttemptCount");
			ReAttemptCount = storage.GetValue<int>("ReAttemptCount");
			TimeOutInterval = storage.GetValue<TimeSpan>("TimeOutInterval");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("WorkingTime", WorkingTime.Save());
			storage.SetValue("Interval", Interval);
			storage.SetValue("AttemptCount", AttemptCount);
			storage.SetValue("ReAttemptCount", ReAttemptCount);
			storage.SetValue("TimeOutInterval", TimeOutInterval);
		}
	}
}