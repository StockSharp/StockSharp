namespace StockSharp.Algo.Strategies.Protective;

/// <summary>
/// Protective controller.
/// </summary>
public class ProtectiveController : BaseLogReceiver
{
	private class ProtectivePositionController : BaseLogReceiver, IProtectivePositionController
	{
		private readonly ProtectiveController _owner;
		private readonly IProtectiveBehaviour _behaviour;

		public ProtectivePositionController(
			ProtectiveController owner,
			SecurityId securityId, string portfolioName,
			IProtectiveBehaviourFactory factory,
			Unit takeValue, Unit stopValue,
			bool isStopTrailing,
			TimeSpan takeTimeout, TimeSpan stopTimeout,
			bool useMarketOrders)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));

			if (factory is null)
				throw new ArgumentNullException(nameof(factory));

			SecurityId = securityId;
			PortfolioName = portfolioName;

			_behaviour = factory.Create(takeValue, stopValue, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders);
			_behaviour.Parent = this;
		}

		public SecurityId SecurityId { get; }
		public string PortfolioName { get; }

		public decimal Position { get; private set; }

		public (bool, Sides, decimal, decimal, OrderCondition)? Update(decimal price, decimal value, DateTime time)
		{
			var info = _behaviour.Update(price, value, time);
			Position += value;

			if (Position != 0m)
				return info;

			_owner.RemoveController(this);
			return null;
		}

		public (bool, Sides, decimal, decimal, OrderCondition)? TryActivate(decimal price, DateTime time)
			=> _behaviour.TryActivate(price, time);
	}

	private readonly SynchronizedDictionary<SecurityId, CachedSynchronizedDictionary<string, ProtectivePositionController>> _controllers = [];

	/// <summary>
	/// Get <see cref="IProtectivePositionController"/> instance.
	/// </summary>
	/// <remarks>
	/// An open position keeps its original protection settings. After it is fully closed, the next call creates a controller using the supplied settings.
	/// </remarks>
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
		=> _controllers.SafeAdd(securityId, key => new(StringComparer.InvariantCultureIgnoreCase)).SafeAdd(portfolioName.ThrowIfEmpty(nameof(portfolioName)),
			key => new(
				this,
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
	public IEnumerable<(bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)> TryActivate(SecurityId securityId, decimal price, DateTime time)
	{
		if (price <= 0)
			yield break;

		if (!_controllers.TryGetValue(securityId, out var dict))
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
		=> _controllers.Clear();

	private void RemoveController(ProtectivePositionController controller)
	{
		if (!_controllers.TryGetValue(controller.SecurityId, out var portfolios))
			return;

		using (portfolios.EnterScope())
		{
			if (portfolios.TryGetValue(controller.PortfolioName, out var current) && ReferenceEquals(current, controller))
				portfolios.Remove(controller.PortfolioName);
		}
	}
}