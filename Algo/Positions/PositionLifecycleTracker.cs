namespace StockSharp.Algo.Positions;

using Ecng.Collections;
using Ecng.Common;

using StockSharp.BusinessEntities;
using StockSharp.Reporting;

/// <summary>
/// Tracks position lifecycle (open/close) via <see cref="ISubscriptionProvider.PositionReceived"/> and stores round-trip history.
/// </summary>
public class PositionLifecycleTracker : Disposable
{
	private class OpenState
	{
		public DateTime OpenTime;
		public decimal? OpenPrice;
		public decimal MaxPosition;
		public decimal LastValue;
	}

	private readonly Dictionary<(SecurityId, string), OpenState> _openPositions = new();
	private readonly CachedSynchronizedList<ReportPosition> _history = new();
	private readonly ISubscriptionProvider _provider;

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionLifecycleTracker"/>.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	public PositionLifecycleTracker(ISubscriptionProvider provider)
	{
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_provider.PositionReceived += OnPositionReceived;
	}

	/// <summary>
	/// Raised when a round-trip is closed.
	/// </summary>
	public event Action<ReportPosition> RoundTripClosed;

	/// <summary>
	/// Completed round-trips history.
	/// </summary>
	public IReadOnlyList<ReportPosition> History => _history.Cache;

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_provider.PositionReceived -= OnPositionReceived;
		base.DisposeManaged();
	}

	private void CloseRoundTrip((SecurityId secId, string pfName) key, OpenState state, DateTime closeTime, decimal? closePrice)
	{
		var rt = new ReportPosition(
			key.secId, key.pfName,
			state.OpenTime, state.OpenPrice,
			closeTime, closePrice,
			state.MaxPosition
		);
		_history.Add(rt);
		_openPositions.Remove(key);
		RoundTripClosed?.Invoke(rt);
	}

	private void OnPositionReceived(Subscription sub, Position position)
	{
		var currentValue = position.CurrentValue ?? 0;
		var key = (secId: position.Security.ToSecurityId(), pfName: position.PortfolioName);

		using (_history.EnterScope())
		{
			if (_openPositions.TryGetValue(key, out var state))
			{
				if (currentValue == 0)
				{
					// position fully closed
					CloseRoundTrip(key, state, position.ServerTime, position.CurrentPrice);
				}
				else if (currentValue.Sign() != state.LastValue.Sign())
				{
					// sign changed — reversal: close current, open new
					CloseRoundTrip(key, state, position.ServerTime, position.CurrentPrice);

					_openPositions[key] = new()
					{
						OpenTime = position.ServerTime,
						OpenPrice = position.CurrentPrice,
						MaxPosition = currentValue.Abs(),
						LastValue = currentValue,
					};
				}
				else
				{
					// same direction, update max
					state.MaxPosition = state.MaxPosition.Max(currentValue.Abs());
					state.LastValue = currentValue;
				}
			}
			else if (currentValue != 0)
			{
				// new position opened
				_openPositions[key] = new()
				{
					OpenTime = position.ServerTime,
					OpenPrice = position.CurrentPrice,
					MaxPosition = currentValue.Abs(),
					LastValue = currentValue,
				};
			}
		}
	}
}
