namespace StockSharp.Algo.Strategies.Protective;

/// <summary>
/// Protective controller.
/// </summary>
public class ProtectiveController : BaseLogReceiver
{
	private class ProtectivePositionController : BaseLogReceiver, IProtectivePositionController
	{
		private readonly IProtectiveBehaviour _behaviour;

		public ProtectivePositionController(
			SecurityId securityId, string portfolioName,
			IProtectiveBehaviourFactory factory,
			Unit takeValue, Unit stopValue,
			bool isStopTrailing,
			TimeSpan takeTimeout, TimeSpan stopTimeout,
			bool useMarketOrders)
		{
			if (factory is null)
				throw new ArgumentNullException(nameof(factory));

			SecurityId = securityId;
			PortfolioName = portfolioName;

			_behaviour = factory.Create(takeValue, stopValue, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders);
			_behaviour.Parent = this;
		}

		public SecurityId SecurityId { get; }
		public string PortfolioName { get; }

		public decimal Position => _behaviour.Position;

		public (bool, Sides, decimal, decimal, OrderCondition)? Update(decimal price, decimal value, DateTimeOffset time)
			=> _behaviour.Update(price, value, time);

		public (bool, Sides, decimal, decimal, OrderCondition)? TryActivate(decimal price, DateTimeOffset time)
			=> _behaviour.TryActivate(price, time);
	}

	private readonly SynchronizedDictionary<SecurityId, CachedSynchronizedDictionary<string, ProtectivePositionController>> _contollers = [];

	/// <summary>
	/// Get <see cref="IProtectivePositionController"/> instance.
	/// </summary>
	/// <param name="securityId"><see cref="IProtectivePositionController.SecurityId"/></param>
	/// <param name="portfolioName"><see cref="IProtectivePositionController.PortfolioName"/></param>
	/// <param name="factory"><see cref="IProtectiveBehaviourFactory"/></param>
	/// <param name="takeValue">Take offset.</param>
	/// <param name="stopValue">Stop offset.</param>
	/// <param name="isStopTrailing">Whether to use a trailing technique.</param>
	/// <param name="takeTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="stopTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="useMarketOrders">Whether to use market orders.</param>
	/// <returns><see cref="IProtectivePositionController"/></returns>
	public IProtectivePositionController GetController(
		SecurityId securityId, string portfolioName,
		IProtectiveBehaviourFactory factory,
		Unit takeValue, Unit stopValue,
		bool isStopTrailing,
		TimeSpan takeTimeout, TimeSpan stopTimeout,
		bool useMarketOrders)
		=> _contollers.SafeAdd(securityId, key => new(StringComparer.InvariantCultureIgnoreCase)).SafeAdd(portfolioName.ThrowIfEmpty(nameof(portfolioName)),
			key => new(
				securityId, portfolioName,
				factory,
				takeValue, stopValue,
				isStopTrailing,
				takeTimeout, stopTimeout,
				useMarketOrders
			) { Parent = this });

	/// <summary>
	/// Try activate protection.
	/// </summary>
	/// <param name="securityId"><see cref="IProtectivePositionController.SecurityId"/></param>
	/// <param name="price">Current price.</param>
	/// <param name="time">Current time.</param>
	/// <returns>Registration order info.</returns>
	public IEnumerable<(bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)> TryActivate(SecurityId securityId, decimal price, DateTimeOffset time)
	{
		if (price <= 0)
			yield break;

		if (!_contollers.TryGetValue(securityId, out var dict))
			yield break;

		foreach (var controller in dict.CachedValues)
		{
			var info = controller.TryActivate(price, time);

			if (info is not null)
				yield return info.Value;
		}
	}

	/// <summary>
	/// Clear state.
	/// </summary>
	public void Clear()
		=> _contollers.Clear();
}