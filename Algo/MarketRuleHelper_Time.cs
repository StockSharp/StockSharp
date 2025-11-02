namespace StockSharp.Algo;

partial class MarketRuleHelper
{
	private class IntervalTimeRule : MarketRule<ITimeProvider, ITimeProvider>
	{
		private readonly MarketTimer _timer;

		public IntervalTimeRule(ITimeProvider provider, TimeSpan interval/*, bool firstTimeRun*/)
			: base(provider)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			Name = LocalizedStrings.Interval + " " + interval;

			_timer = new MarketTimer(provider, () => Activate(provider))
				.Interval(interval)
				.Start();
		}

		protected override void DisposeManaged()
		{
			_timer.Dispose();
			base.DisposeManaged();
		}
	}

	private class TimeComeRule : MarketRule<ITimeProvider, DateTime>
	{
		private readonly MarketTimer _timer;

		public TimeComeRule(ITimeProvider provider, IEnumerable<DateTime> times)
			: base(provider)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			if (times == null)
				throw new ArgumentNullException(nameof(times));

			var currentTime = provider.CurrentTimeUtc;

			var intervals = new SynchronizedQueue<TimeSpan>();
			var timesList = new SynchronizedList<DateTime>();

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

			_timer = new MarketTimer(provider, () =>
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
	/// <param name="provider"><see cref="ITimeProvider"/></param>
	/// <param name="times">The exact time. Several values may be sent.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<ITimeProvider, DateTime> WhenTimeCome(this ITimeProvider provider, params DateTime[] times)
		=> provider.WhenTimeCome((IEnumerable<DateTime>)times);

	/// <summary>
	/// To create a rule, activated at the exact time, specified through <paramref name="times" />.
	/// </summary>
	/// <param name="provider"><see cref="ITimeProvider"/></param>
	/// <param name="times">The exact time. Several values may be sent.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<ITimeProvider, DateTime> WhenTimeCome(this ITimeProvider provider, IEnumerable<DateTime> times)
		=> new TimeComeRule(provider, times);

	/// <summary>
	/// To create a rule for the event <see cref="ITimeProvider.CurrentTimeChanged"/>, activated after expiration of <paramref name="interval" />.
	/// </summary>
	/// <param name="provider"><see cref="ITimeProvider"/></param>
	/// <param name="interval">Interval.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<ITimeProvider, ITimeProvider> WhenIntervalElapsed(this ITimeProvider provider, TimeSpan interval)
		=> new IntervalTimeRule(provider, interval);
}