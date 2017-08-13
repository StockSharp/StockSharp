#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Algo
File: TimeFrameStrategy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		/// <param name="timeFrame">The strategy timeframe.</param>
		protected TimeFrameStrategy(TimeSpan timeFrame)
		{
			_interval = this.Param(nameof(Interval), timeFrame);
			_timeFrame = this.Param(nameof(TimeFrame), timeFrame);
		}

		private readonly StrategyParam<TimeSpan> _timeFrame;

		/// <summary>
		/// The strategy timeframe.
		/// </summary>
		public TimeSpan TimeFrame
        {
			get => _timeFrame.Value;
			set => _timeFrame.Value = value;
		}

		private readonly StrategyParam<TimeSpan> _interval;

		/// <summary>
		/// The strategy start-up interval. By default, it equals to <see cref="TimeFrame"/>.
		/// </summary>
		public TimeSpan Interval
		{
			get => _interval.Value;
			set => _interval.Value = value;
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