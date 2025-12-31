namespace StockSharp.Algo;

/// <summary>
/// The timer, based on trading system time.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MarketTimer"/>.
/// </remarks>
/// <param name="provider"><see cref="ITimeProvider"/></param>
/// <param name="activated">The timer processor.</param>
public class MarketTimer(ITimeProvider provider, Action activated) : Disposable
{
	private readonly ITimeProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
	private readonly Action _activated = activated ?? throw new ArgumentNullException(nameof(activated));
	private bool _started;
	private TimeSpan _interval;
	private readonly Lock _syncLock = new();
	private TimeSpan _elapsedTime;

	/// <summary>
	/// To set the interval.
	/// </summary>
	/// <param name="interval">The timer interval. If <see cref="TimeSpan.Zero"/> value is set, timer stops to be periodical.</param>
	/// <returns>The timer.</returns>
	public MarketTimer Interval(TimeSpan interval)
	{
		if (interval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), interval, LocalizedStrings.InvalidValue);

		using (_syncLock.EnterScope())
		{
			_interval = interval;
			_elapsedTime = TimeSpan.Zero;
		}
		
		return this;
	}

	/// <summary>
	/// To start the timer.
	/// </summary>
	/// <returns>The timer.</returns>
	public MarketTimer Start()
	{
		if (_interval == default)
			throw new InvalidOperationException(LocalizedStrings.IntervalNotSet);

		using (_syncLock.EnterScope())
		{
			if (!_started)
			{
				_started = true;
				_provider.CurrentTimeChanged += OnCurrentTimeChanged;
			}
		}

		return this;
	}

	/// <summary>
	/// To stop the timer.
	/// </summary>
	/// <returns>The timer.</returns>
	public MarketTimer Stop()
	{
		using (_syncLock.EnterScope())
		{
			if (_started)
			{
				_started = false;
				_provider.CurrentTimeChanged -= OnCurrentTimeChanged;
			}	
		}
		
		return this;
	}

	private void OnCurrentTimeChanged(TimeSpan diff)
	{
		Action act;

		using (_syncLock.EnterScope())
		{
			if (!_started)
				return;

			_elapsedTime += diff;

			if (_elapsedTime < _interval)
				return;

			_elapsedTime = TimeSpan.Zero;
			act = _activated;
		}

		act();
	}

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		Stop();
		base.DisposeManaged();
	}
}