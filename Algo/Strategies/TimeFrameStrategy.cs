namespace StockSharp.Algo.Strategies
{
	using System;

	/// <summary>
	/// Results of the trading strategy one iteration operation.
	/// </summary>
	public enum ProcessResults
	{
		/// <summary>
		/// To continue the operation.
		/// </summary>
		Continue,

		/// <summary>
		/// To stop the strategy operation.
		/// </summary>
		Stop,
	}

	/// <summary>
	/// The timeframe based trade strategy.
	/// </summary>
	public abstract class TimeFrameStrategy : Strategy
	{
	    /// <summary>
	    /// Initialize <see cref="TimeFrameStrategy"/>.
	    /// </summary>
	    /// <param name="timeFrame">The startegy timeframe.</param>
	    protected TimeFrameStrategy(TimeSpan timeFrame)
		{
			_interval = this.Param("Interval", timeFrame);
			_timeFrame = this.Param("TimeFrame", timeFrame);
		}

		private readonly StrategyParam<TimeSpan> _timeFrame;

        /// <summary>
        /// The startegy timeframe.
        /// </summary>
        public TimeSpan TimeFrame
        {
			get { return _timeFrame.Value; }
            set { _timeFrame.Value = value; }
        }

		private readonly StrategyParam<TimeSpan> _interval;

		/// <summary>
		/// The startegy start-up interval. By default, it equals to <see cref="TimeFrameStrategy.TimeFrame"/>.
		/// </summary>
		public TimeSpan Interval
		{
			get { return _interval.Value; }
			set { _interval.Value = value; }
		}

		/// <summary>
		/// The method is called when the <see cref="Strategy.Start"/> method has been called and the <see cref="Strategy.ProcessState"/> state has been taken the <see cref="ProcessStates.Started"/> value.
		/// </summary>
		protected override void OnStarted()
		{
			base.OnStarted();

			SafeGetConnector()
				.WhenIntervalElapsed(Interval/*, true*/)
				.Do(() =>
				{
					var result = OnProcess();

					if (result == ProcessResults.Stop)
						Stop();
				})
				.Until(() =>
				{
					if (ProcessState == ProcessStates.Stopping)
						return OnProcess() == ProcessResults.Stop;

					return false;
				})
				.Apply(this);
		}

		/// <summary>
		/// The implementation of the trade algorithm.
		/// </summary>
		/// <returns>The result of trade algorithm one iteration operation.</returns>
		protected abstract ProcessResults OnProcess();
	}
}