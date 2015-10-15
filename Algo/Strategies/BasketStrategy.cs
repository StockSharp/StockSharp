namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Conditions of subsidiary strategies operation end.
	/// </summary>
	public enum BasketStrategyFinishModes
	{
		/// <summary>
		/// If at least one strategy ended.
		/// </summary>
		First,

		/// <summary>
		/// If all strategies ended.
		/// </summary>
		All,

		/// <summary>
		/// Subsidiary strategies do not depend on each other.
		/// </summary>
		None,
	}

	/// <summary>
	/// The batch strategy, containing subsidiary strategies, affecting each other by their execution.
	/// </summary>
	public class BasketStrategy : Strategy
	{
		/// <summary>
		/// Create strategy.
		/// </summary>
		/// <param name="finishMode">The condition of subsidiary strategies operation end.</param>
		public BasketStrategy(BasketStrategyFinishModes finishMode)
		{
			FinishMode = finishMode;

			if (FinishMode != BasketStrategyFinishModes.None)
				ChildStrategies.Added += OnChildStrategiesAdded;
		}

		/// <summary>
		/// The condition of subsidiary strategies operation end.
		/// </summary>
		public BasketStrategyFinishModes FinishMode { get; private set; }

		/// <summary>
		/// First stopped subsidiary strategy. The property is filled at <see cref="BasketStrategy.FinishMode"/> equals to <see cref="BasketStrategyFinishModes.First"/>.
		/// </summary>
		public Strategy FirstFinishStrategy { get; private set; }

		/// <summary>
		/// The method is called when the <see cref="Strategy.Start"/> method has been called and the <see cref="Strategy.ProcessState"/> state has been taken the <see cref="ProcessStates.Started"/> value.
		/// </summary>
		protected override void OnStarted()
		{
			if (FinishMode != BasketStrategyFinishModes.None && ChildStrategies.Count == 0)
				throw new InvalidOperationException(LocalizedStrings.Str1224);

			base.OnStarted();
		}

		private void OnChildStrategiesAdded(Strategy strategy)
		{
			var rule = strategy
				.WhenStopped()
				.Do(s =>
				{
					if (FinishMode == BasketStrategyFinishModes.First)
					{
						if (FirstFinishStrategy == null)
						{
							FirstFinishStrategy = s;
							Stop();
						}
					}
					else
					{
						if (ChildStrategies.SyncGet(c => c.All(child => child.ProcessState != ProcessStates.Started)))
							Stop();
					}
				})
				.Once()
				.Apply(this);

			rule.UpdateName(rule.Name + " (BasketStrategy.OnChildStrategiesAdded)");
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (FinishMode != BasketStrategyFinishModes.None)
				ChildStrategies.Added -= OnChildStrategiesAdded;

			base.DisposeManaged();
		}
	}
}