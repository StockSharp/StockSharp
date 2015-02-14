namespace StockSharp.Algo.Strategies
{
	using System;

	/// <summary>
	/// Результаты работы одной итерации торговой стратегии.
	/// </summary>
	public enum ProcessResults
	{
		/// <summary>
		/// Продолжить работу дальше.
		/// </summary>
		Continue,

		/// <summary>
		/// Прекратить работу стратегии.
		/// </summary>
		Stop,
	}

	/// <summary>
	/// Торговая стратегия, основанное на тайм-фрейме.
	/// </summary>
	public abstract class TimeFrameStrategy : Strategy
	{
	    /// <summary>
	    /// Инициализировать <see cref="TimeFrameStrategy"/>.
	    /// </summary>
	    /// <param name="timeFrame">Таймфрейм стратегии.</param>
	    protected TimeFrameStrategy(TimeSpan timeFrame)
		{
			_interval = this.Param("Interval", timeFrame);
			_timeFrame = this.Param("TimeFrame", timeFrame);
		}

		private readonly StrategyParam<TimeSpan> _timeFrame;

        /// <summary>
        /// Таймфрейм стратегии.
        /// </summary>
        public TimeSpan TimeFrame
        {
			get { return _timeFrame.Value; }
            set { _timeFrame.Value = value; }
        }

		private readonly StrategyParam<TimeSpan> _interval;

		/// <summary>
		/// Интервал запуска стратегии. По умолчанию равен <see cref="TimeFrame"/>.
		/// </summary>
		public TimeSpan Interval
		{
			get { return _interval.Value; }
			set { _interval.Value = value; }
		}

		/// <summary>
		/// Метод вызывается тогда, когда вызвался метод <see cref="Strategy.Start"/>, и состояние <see cref="Strategy.ProcessState"/> перешло в значение <see cref="ProcessStates.Started"/>.
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
		/// Реализация торгового алгоритма.
		/// </summary>
		/// <returns>
		/// Результат работы одной итерации торгового алгоритма.
		/// </returns>
		protected abstract ProcessResults OnProcess();
	}
}