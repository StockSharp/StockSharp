namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Условия окончания работы дочерних стратегий.
	/// </summary>
	public enum BasketStrategyFinishModes
	{
		/// <summary>
		/// Если закончилась хотя бы одна стратегия.
		/// </summary>
		First,

		/// <summary>
		/// Если закончились все стратегии.
		/// </summary>
		All,

		/// <summary>
		/// Дочерние стратегии никак не зависят друг на друга.
		/// </summary>
		None,
	}

	/// <summary>
	/// Пакетная стратегия, содержащая в себе дочерние стратегии, которые влияют друг на друга своим исполнением.
	/// </summary>
	public class BasketStrategy : Strategy
	{
		/// <summary>
		/// Создать стратегию.
		/// </summary>
		/// <param name="finishMode">Условие окончания работы дочерних стратегий.</param>
		public BasketStrategy(BasketStrategyFinishModes finishMode)
		{
			FinishMode = finishMode;

			if (FinishMode != BasketStrategyFinishModes.None)
				ChildStrategies.Added += OnChildStrategiesAdded;
		}

		/// <summary>
		/// Условие окончания работы дочерних стратегий.
		/// </summary>
		public BasketStrategyFinishModes FinishMode { get; private set; }

		/// <summary>
		/// Первая остановившаяся дочерняя стратегия. Свойство заполняется при <see cref="FinishMode"/> равным <see cref="BasketStrategyFinishModes.First"/>.
		/// </summary>
		public Strategy FirstFinishStrategy { get; private set; }

		/// <summary>
		/// Метод вызывается тогда, когда вызвался метод <see cref="Strategy.Start"/>, и состояние <see cref="Strategy.ProcessState"/> перешло в значение <see cref="ProcessStates.Started"/>.
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
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (FinishMode != BasketStrategyFinishModes.None)
				ChildStrategies.Added -= OnChildStrategiesAdded;

			base.DisposeManaged();
		}
	}
}