#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Algo
File: BasketStrategy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Linq;

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
		public BasketStrategyFinishModes FinishMode { get; }

		/// <summary>
		/// First stopped subsidiary strategy. The property is filled at <see cref="FinishMode"/> equals to <see cref="BasketStrategyFinishModes.First"/>.
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
						if (ChildStrategies.All(child => child.ProcessState != ProcessStates.Started))
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