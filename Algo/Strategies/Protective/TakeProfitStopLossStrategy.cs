namespace StockSharp.Algo.Strategies.Protective
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The strategy protecting trades together by strategies <see cref="TakeProfitStrategy"/> and <see cref="StopLossStrategy"/>.
	/// </summary>
	[Obsolete("Use ProtectiveController class.")]
	public class TakeProfitStopLossStrategy : BasketStrategy, IProtectiveStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TakeProfitStopLossStrategy"/>.
		/// </summary>
		/// <param name="takeProfit">Profit protection strategy.</param>
		/// <param name="stopLoss">The loss protection strategy.</param>
		public TakeProfitStopLossStrategy(TakeProfitStrategy takeProfit, StopLossStrategy stopLoss)
			: base(BasketStrategyFinishModes.All)
		{
			if (takeProfit == null)
				throw new ArgumentNullException(nameof(takeProfit));

			if (stopLoss == null)
				throw new ArgumentNullException(nameof(stopLoss));

			ApplyRule(takeProfit, stopLoss);
			ApplyRule(stopLoss, takeProfit);
		}

		private void ApplyRule(ProtectiveStrategy main, ProtectiveStrategy opposite)
		{
			main
				.WhenActivated()
				.Do(opposite.Stop)
				.Apply(this);

			ChildStrategies.Add(main);
		}

		private IProtectiveStrategy FirstStrategy => (IProtectiveStrategy)ChildStrategies.First();

		/// <inheritdoc />
		public decimal ProtectiveVolume
		{
			get => FirstStrategy.ProtectiveVolume;
			set
			{
				ChildStrategies.OfType<IProtectiveStrategy>().ForEach(s => s.ProtectiveVolume = value);
				ProtectiveVolumeChanged?.Invoke();
			}
		}

		/// <inheritdoc />
		public decimal ProtectivePrice => FirstStrategy.ProtectivePrice;

		/// <inheritdoc />
		public Sides ProtectiveSide => FirstStrategy.ProtectiveSide;

		/// <inheritdoc />
		public event Action ProtectiveVolumeChanged;
	}
}