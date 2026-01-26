namespace StockSharp.Algo.Latency;

/// <summary>
/// Default implementation of <see cref="ILatencyManagerState"/>.
/// </summary>
public class LatencyManagerState : ILatencyManagerState
{
	private readonly Lock _sync = new();
	private readonly Dictionary<long, DateTime> _registrations = [];
	private readonly Dictionary<long, DateTime> _cancellations = [];

	private TimeSpan _latencyRegistration;
	private TimeSpan _latencyCancellation;

	/// <inheritdoc />
	public TimeSpan LatencyRegistration
	{
		get
		{
			using (_sync.EnterScope())
				return _latencyRegistration;
		}
	}

	/// <inheritdoc />
	public TimeSpan LatencyCancellation
	{
		get
		{
			using (_sync.EnterScope())
				return _latencyCancellation;
		}
	}

	/// <inheritdoc />
	public void AddRegistration(long transactionId, DateTime localTime)
	{
		if (transactionId == 0)
			throw new ArgumentOutOfRangeException(nameof(transactionId), transactionId, LocalizedStrings.InvalidValue);

		if (localTime == default)
			throw new ArgumentOutOfRangeException(nameof(localTime), localTime, LocalizedStrings.InvalidValue);

		using (_sync.EnterScope())
		{
			if (_registrations.ContainsKey(transactionId))
				throw new ArgumentException(LocalizedStrings.TransactionRegAlreadyAdded.Put(transactionId), nameof(transactionId));

			_registrations.Add(transactionId, localTime);
		}
	}

	/// <inheritdoc />
	public bool TryGetAndRemoveRegistration(long transactionId, out DateTime localTime)
	{
		using (_sync.EnterScope())
		{
			if (_registrations.TryGetValue(transactionId, out localTime))
			{
				_registrations.Remove(transactionId);
				return true;
			}

			return false;
		}
	}

	/// <inheritdoc />
	public void AddCancellation(long transactionId, DateTime localTime)
	{
		if (transactionId <= 0)
			throw new ArgumentOutOfRangeException(nameof(transactionId), transactionId, LocalizedStrings.InvalidValue);

		if (localTime == default)
			throw new ArgumentOutOfRangeException(nameof(localTime), localTime, LocalizedStrings.InvalidValue);

		using (_sync.EnterScope())
		{
			if (_cancellations.ContainsKey(transactionId))
				throw new ArgumentException(LocalizedStrings.TransactionCancelAlreadyAdded.Put(transactionId), nameof(transactionId));

			_cancellations.Add(transactionId, localTime);
		}
	}

	/// <inheritdoc />
	public bool TryGetAndRemoveCancellation(long transactionId, out DateTime localTime)
	{
		using (_sync.EnterScope())
		{
			if (_cancellations.TryGetValue(transactionId, out localTime))
			{
				_cancellations.Remove(transactionId);
				return true;
			}

			return false;
		}
	}

	/// <inheritdoc />
	public void AddLatencyRegistration(TimeSpan latency)
	{
		using (_sync.EnterScope())
			_latencyRegistration += latency;
	}

	/// <inheritdoc />
	public void AddLatencyCancellation(TimeSpan latency)
	{
		using (_sync.EnterScope())
			_latencyCancellation += latency;
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_registrations.Clear();
			_cancellations.Clear();
			_latencyRegistration = default;
			_latencyCancellation = default;
		}
	}
}
