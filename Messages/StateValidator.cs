namespace StockSharp.Messages;

using Ecng.Reflection;

/// <summary>
/// Unified state transition validator for all state enums.
/// </summary>
/// <remarks>
/// Provides centralized validation of state transitions with configurable behavior:
/// - Returns bool for simple validation
/// - Logs warnings for invalid transitions
/// - Throws exceptions when required
/// </remarks>
public static class StateValidator
{
	#region StateMatrix

	private sealed class StateMatrix<T>
		where T : struct, Enum
	{
		private readonly bool[][] _map;
		private readonly Func<T, int> _converter;

		public StateMatrix(Func<T, int> converter)
		{
			_converter = converter ?? throw new ArgumentNullException(nameof(converter));

			var count = Enumerator.GetValues<T>().Count();
			_map = new bool[count][];

			for (var i = 0; i < _map.Length; i++)
				_map[i] = new bool[count];
		}

		public bool this[T from, T to]
		{
			get => _map[_converter(from)][_converter(to)];
			set => _map[_converter(from)][_converter(to)] = value;
		}
	}

	#endregion

	#region Matrices

	private static readonly StateMatrix<OrderStates> _orderStates;
	private static readonly StateMatrix<ChannelStates> _channelStates;
	private static readonly StateMatrix<SubscriptionStates> _subscriptionStates;
	private static readonly StateMatrix<SessionStates> _sessionStates;

	static StateValidator()
	{
		// OrderStates transitions
		// None -> Pending, Active, Done, Failed
		// Pending -> Active, Failed
		// Active -> Done
		// Done, Failed are terminal states
		_orderStates = new(s => (int)s);
		_orderStates[OrderStates.None, OrderStates.Pending] = true;
		_orderStates[OrderStates.None, OrderStates.Active] = true;
		_orderStates[OrderStates.None, OrderStates.Done] = true;
		_orderStates[OrderStates.None, OrderStates.Failed] = true;
		_orderStates[OrderStates.Pending, OrderStates.Active] = true;
		_orderStates[OrderStates.Pending, OrderStates.Failed] = true;
		_orderStates[OrderStates.Active, OrderStates.Done] = true;

		// ChannelStates transitions
		// Stopped <-> Starting <-> Started
		// Started -> Suspending -> Suspended
		// Suspended -> Starting, Stopping
		// Stopping -> Stopped
		_channelStates = new(s => (int)s);
		_channelStates[ChannelStates.Stopped, ChannelStates.Starting] = true;
		_channelStates[ChannelStates.Starting, ChannelStates.Stopped] = true;
		_channelStates[ChannelStates.Starting, ChannelStates.Started] = true;
		_channelStates[ChannelStates.Started, ChannelStates.Stopping] = true;
		_channelStates[ChannelStates.Started, ChannelStates.Suspending] = true;
		_channelStates[ChannelStates.Suspending, ChannelStates.Suspended] = true;
		_channelStates[ChannelStates.Suspended, ChannelStates.Starting] = true;
		_channelStates[ChannelStates.Suspended, ChannelStates.Stopping] = true;
		_channelStates[ChannelStates.Stopping, ChannelStates.Stopped] = true;

		// SubscriptionStates transitions
		// Stopped -> Active, Online, Error, Finished
		// Active -> Online, Stopped, Error, Finished
		// Online -> Stopped, Error, Finished (not back to Active)
		// Error, Finished are terminal states
		_subscriptionStates = new(s => (int)s);
		_subscriptionStates[SubscriptionStates.Stopped, SubscriptionStates.Active] = true;
		_subscriptionStates[SubscriptionStates.Stopped, SubscriptionStates.Online] = true;
		_subscriptionStates[SubscriptionStates.Stopped, SubscriptionStates.Error] = true;
		_subscriptionStates[SubscriptionStates.Stopped, SubscriptionStates.Finished] = true;
		_subscriptionStates[SubscriptionStates.Active, SubscriptionStates.Online] = true;
		_subscriptionStates[SubscriptionStates.Active, SubscriptionStates.Stopped] = true;
		_subscriptionStates[SubscriptionStates.Active, SubscriptionStates.Error] = true;
		_subscriptionStates[SubscriptionStates.Active, SubscriptionStates.Finished] = true;
		_subscriptionStates[SubscriptionStates.Online, SubscriptionStates.Stopped] = true;
		_subscriptionStates[SubscriptionStates.Online, SubscriptionStates.Error] = true;
		_subscriptionStates[SubscriptionStates.Online, SubscriptionStates.Finished] = true;
		// Error and Finished are terminal - no valid transitions out

		// SessionStates transitions
		// Assigned -> Active, Paused, ForceStopped, Ended
		// Active -> Paused, ForceStopped, Ended
		// Paused -> Active, ForceStopped, Ended
		// ForceStopped, Ended are terminal
		_sessionStates = new(s => (int)s);
		_sessionStates[SessionStates.Assigned, SessionStates.Active] = true;
		_sessionStates[SessionStates.Assigned, SessionStates.Paused] = true;
		_sessionStates[SessionStates.Assigned, SessionStates.ForceStopped] = true;
		_sessionStates[SessionStates.Assigned, SessionStates.Ended] = true;
		_sessionStates[SessionStates.Active, SessionStates.Paused] = true;
		_sessionStates[SessionStates.Active, SessionStates.ForceStopped] = true;
		_sessionStates[SessionStates.Active, SessionStates.Ended] = true;
		_sessionStates[SessionStates.Paused, SessionStates.Active] = true;
		_sessionStates[SessionStates.Paused, SessionStates.ForceStopped] = true;
		_sessionStates[SessionStates.Paused, SessionStates.Ended] = true;
	}

	#endregion

	#region IsValid methods (pure bool, no side effects)

	/// <summary>
	/// Check if transition from one <see cref="OrderStates"/> to another is valid.
	/// </summary>
	/// <param name="from">Current state.</param>
	/// <param name="to">New state.</param>
	/// <returns><c>true</c> if transition is valid; otherwise <c>false</c>.</returns>
	public static bool IsValid(OrderStates from, OrderStates to)
		=> from == to || _orderStates[from, to];

	/// <summary>
	/// Check if transition from one <see cref="ChannelStates"/> to another is valid.
	/// </summary>
	/// <param name="from">Current state.</param>
	/// <param name="to">New state.</param>
	/// <returns><c>true</c> if transition is valid; otherwise <c>false</c>.</returns>
	public static bool IsValid(ChannelStates from, ChannelStates to)
		=> from == to || _channelStates[from, to];

	/// <summary>
	/// Check if transition from one <see cref="SubscriptionStates"/> to another is valid.
	/// </summary>
	/// <param name="from">Current state.</param>
	/// <param name="to">New state.</param>
	/// <returns><c>true</c> if transition is valid; otherwise <c>false</c>.</returns>
	public static bool IsValid(SubscriptionStates from, SubscriptionStates to)
		=> from == to || _subscriptionStates[from, to];

	/// <summary>
	/// Check if transition from one <see cref="SessionStates"/> to another is valid.
	/// </summary>
	/// <param name="from">Current state.</param>
	/// <param name="to">New state.</param>
	/// <returns><c>true</c> if transition is valid; otherwise <c>false</c>.</returns>
	public static bool IsValid(SessionStates from, SessionStates to)
		=> from == to || _sessionStates[from, to];

	#endregion

	#region Validate methods (with logging and optional exception)

	/// <summary>
	/// Validate <see cref="OrderStates"/> transition with logging and optional exception.
	/// </summary>
	/// <param name="from">Current state (null means initial state).</param>
	/// <param name="to">New state.</param>
	/// <param name="context">Context for error message (e.g., transaction id).</param>
	/// <param name="logs">Log receiver for warnings.</param>
	/// <param name="throwOnInvalid">If <c>true</c>, throws <see cref="InvalidOperationException"/> on invalid transition.</param>
	/// <returns><c>true</c> if transition is valid; otherwise <c>false</c>.</returns>
	public static bool Validate(OrderStates? from, OrderStates to, object context, ILogReceiver logs, bool throwOnInvalid = false)
	{
		if (from is null || from == to)
			return true;

		var isValid = _orderStates[from.Value, to];

		if (!isValid)
		{
			var message = $"{nameof(OrderStates)} invalid transition: {from} -> {to}. Context: {context}";

			if (throwOnInvalid)
				throw new InvalidOperationException(message);

			logs?.AddWarningLog(message);
		}

		return isValid;
	}

	/// <summary>
	/// Validate <see cref="ChannelStates"/> transition with logging and optional exception.
	/// </summary>
	/// <param name="from">Current state.</param>
	/// <param name="to">New state.</param>
	/// <param name="context">Context for error message.</param>
	/// <param name="logs">Log receiver for warnings.</param>
	/// <param name="throwOnInvalid">If <c>true</c>, throws <see cref="InvalidOperationException"/> on invalid transition.</param>
	/// <returns><c>true</c> if transition is valid; otherwise <c>false</c>.</returns>
	public static bool Validate(ChannelStates from, ChannelStates to, object context, ILogReceiver logs, bool throwOnInvalid = false)
	{
		if (from == to)
			return true;

		var isValid = _channelStates[from, to];

		if (!isValid)
		{
			var message = $"{nameof(ChannelStates)} invalid transition: {from} -> {to}. Context: {context}";

			if (throwOnInvalid)
				throw new InvalidOperationException(message);

			logs?.AddWarningLog(message);
		}

		return isValid;
	}

	/// <summary>
	/// Validate <see cref="SubscriptionStates"/> transition with logging and optional exception.
	/// </summary>
	/// <param name="from">Current state.</param>
	/// <param name="to">New state.</param>
	/// <param name="context">Context for error message (e.g., subscription id).</param>
	/// <param name="logs">Log receiver for warnings.</param>
	/// <param name="throwOnInvalid">If <c>true</c>, throws <see cref="InvalidOperationException"/> on invalid transition.</param>
	/// <returns><c>true</c> if transition is valid; otherwise <c>false</c>.</returns>
	public static bool Validate(SubscriptionStates from, SubscriptionStates to, object context, ILogReceiver logs, bool throwOnInvalid = false)
	{
		if (from == to)
			return true;

		var isValid = _subscriptionStates[from, to];

		if (!isValid)
		{
			var message = $"{nameof(SubscriptionStates)} invalid transition: {from} -> {to}. Context: {context}";

			if (throwOnInvalid)
				throw new InvalidOperationException(message);

			logs?.AddWarningLog(message);
		}

		return isValid;
	}

	/// <summary>
	/// Validate <see cref="SessionStates"/> transition with logging and optional exception.
	/// </summary>
	/// <param name="from">Current state.</param>
	/// <param name="to">New state.</param>
	/// <param name="context">Context for error message.</param>
	/// <param name="logs">Log receiver for warnings.</param>
	/// <param name="throwOnInvalid">If <c>true</c>, throws <see cref="InvalidOperationException"/> on invalid transition.</param>
	/// <returns><c>true</c> if transition is valid; otherwise <c>false</c>.</returns>
	public static bool Validate(SessionStates from, SessionStates to, object context, ILogReceiver logs, bool throwOnInvalid = false)
	{
		if (from == to)
			return true;

		var isValid = _sessionStates[from, to];

		if (!isValid)
		{
			var message = $"{nameof(SessionStates)} invalid transition: {from} -> {to}. Context: {context}";

			if (throwOnInvalid)
				throw new InvalidOperationException(message);

			logs?.AddWarningLog(message);
		}

		return isValid;
	}

	#endregion

	#region Terminal state checks

	/// <summary>
	/// Determines if the <see cref="OrderStates"/> is a terminal (final) state.
	/// </summary>
	/// <param name="state">State to check.</param>
	/// <returns><c>true</c> if state is terminal; otherwise <c>false</c>.</returns>
	public static bool IsTerminal(OrderStates state)
		=> state is OrderStates.Done or OrderStates.Failed;

	/// <summary>
	/// Determines if the <see cref="SubscriptionStates"/> is a terminal (final) state.
	/// </summary>
	/// <param name="state">State to check.</param>
	/// <returns><c>true</c> if state is terminal; otherwise <c>false</c>.</returns>
	public static bool IsTerminal(SubscriptionStates state)
		=> state is SubscriptionStates.Error or SubscriptionStates.Finished;

	/// <summary>
	/// Determines if the <see cref="SessionStates"/> is a terminal (final) state.
	/// </summary>
	/// <param name="state">State to check.</param>
	/// <returns><c>true</c> if state is terminal; otherwise <c>false</c>.</returns>
	public static bool IsTerminal(SessionStates state)
		=> state is SessionStates.ForceStopped or SessionStates.Ended;

	/// <summary>
	/// Determines if the <see cref="ChannelStates"/> is a stopped state.
	/// </summary>
	/// <param name="state">State to check.</param>
	/// <returns><c>true</c> if state is stopped; otherwise <c>false</c>.</returns>
	public static bool IsStopped(ChannelStates state)
		=> state == ChannelStates.Stopped;

	#endregion

	#region Active state checks

	/// <summary>
	/// Determines if the <see cref="OrderStates"/> is an active (working) state.
	/// </summary>
	/// <param name="state">State to check.</param>
	/// <returns><c>true</c> if state is active; otherwise <c>false</c>.</returns>
	public static bool IsActive(OrderStates state)
		=> state is OrderStates.Pending or OrderStates.Active;

	/// <summary>
	/// Determines if the <see cref="SubscriptionStates"/> is an active (working) state.
	/// </summary>
	/// <param name="state">State to check.</param>
	/// <returns><c>true</c> if state is active; otherwise <c>false</c>.</returns>
	public static bool IsActive(SubscriptionStates state)
		=> state is SubscriptionStates.Active or SubscriptionStates.Online;

	/// <summary>
	/// Determines if the <see cref="ChannelStates"/> is an active (working) state.
	/// </summary>
	/// <param name="state">State to check.</param>
	/// <returns><c>true</c> if state is active; otherwise <c>false</c>.</returns>
	public static bool IsActive(ChannelStates state)
		=> state == ChannelStates.Started;

	/// <summary>
	/// Determines if the <see cref="SessionStates"/> is an active (working) state.
	/// </summary>
	/// <param name="state">State to check.</param>
	/// <returns><c>true</c> if state is active; otherwise <c>false</c>.</returns>
	public static bool IsActive(SessionStates state)
		=> state == SessionStates.Active;

	#endregion
}
