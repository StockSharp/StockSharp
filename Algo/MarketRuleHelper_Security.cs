namespace StockSharp.Algo;

partial class MarketRuleHelper
{
	private sealed class TimeComeRule : MarketRule<IConnector, DateTimeOffset>
	{
		private readonly MarketTimer _timer;

		public TimeComeRule(IConnector connector, IEnumerable<DateTimeOffset> times)
			: base(connector)
		{
			if (times == null)
				throw new ArgumentNullException(nameof(times));

			var currentTime = connector.CurrentTime;

			var intervals = new SynchronizedQueue<TimeSpan>();
			var timesList = new SynchronizedList<DateTimeOffset>();

			foreach (var time in times)
			{
				var interval = time - currentTime;

				if (interval <= TimeSpan.Zero)
					continue;

				intervals.Enqueue(interval);
				currentTime = time;
				timesList.Add(time);
			}

			// все даты устарели
			if (timesList.IsEmpty())
				return;

			Name = LocalizedStrings.Time;

			var index = 0;

			_timer = new MarketTimer(connector, () =>
			{
				var activateTime = timesList[index++];

				Activate(activateTime);

				if (index == timesList.Count)
				{
					_timer.Stop();
				}
				else
				{
					_timer.Interval(intervals.Dequeue());
				}
			})
			.Interval(intervals.Dequeue())
			.Start();
		}

		protected override bool CanFinish()
		{
			return _timer == null || base.CanFinish();
		}

		protected override void DisposeManaged()
		{
			_timer?.Dispose();

			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule, activated at the exact time, specified through <paramref name="times" />.
	/// </summary>
	/// <param name="connector">Connection to the trading system.</param>
	/// <param name="times">The exact time. Several values may be sent.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<IConnector, DateTimeOffset> WhenTimeCome(this IConnector connector, params DateTimeOffset[] times)
	{
		return connector.WhenTimeCome((IEnumerable<DateTimeOffset>)times);
	}

	/// <summary>
	/// To create a rule, activated at the exact time, specified through <paramref name="times" />.
	/// </summary>
	/// <param name="connector">Connection to the trading system.</param>
	/// <param name="times">The exact time. Several values may be sent.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<IConnector, DateTimeOffset> WhenTimeCome(this IConnector connector, IEnumerable<DateTimeOffset> times)
	{
		return new TimeComeRule(connector, times);
	}
}
