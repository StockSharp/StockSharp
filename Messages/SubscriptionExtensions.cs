namespace StockSharp.Messages;

/// <summary>
/// <see cref="ISubscriptionMessage"/> extensions.
/// </summary>
public static class SubscriptionExtensions
{
	/// <summary>
	/// Determines the specified state equals <see cref="SubscriptionStates.Active"/> or <see cref="SubscriptionStates.Online"/>.
	/// </summary>
	/// <param name="state">State.</param>
	/// <returns>Check result.</returns>
	public static bool IsActive(this SubscriptionStates state)
	{
		return state is SubscriptionStates.Active or SubscriptionStates.Online;
	}

	/// <summary>
	/// Change subscription state.
	/// </summary>
	/// <param name="currState">Current state.</param>
	/// <param name="newState">New state.</param>
	/// <param name="subscriptionId">Subscription id.</param>
	/// <param name="receiver">Logs.</param>
	/// <returns>New state.</returns>
	public static SubscriptionStates ChangeSubscriptionState(this SubscriptionStates currState, SubscriptionStates newState, long subscriptionId, ILogReceiver receiver)
	{
		bool isOk;

		if (currState == newState)
			isOk = false;
		else
		{
			isOk = currState switch
			{
				SubscriptionStates.Stopped or SubscriptionStates.Active => true,
				SubscriptionStates.Error or SubscriptionStates.Finished => false,
				SubscriptionStates.Online => newState != SubscriptionStates.Active,
				_ => throw new ArgumentOutOfRangeException(nameof(currState), currState, LocalizedStrings.InvalidValue),
			};
		}

		const string text = "Subscription {0} {1}->{2}.";

		if (isOk)
			receiver.AddDebugLog(text, subscriptionId, currState, newState);
		else
			receiver.AddWarningLog(text, subscriptionId, currState, newState);

		return newState;
	}

	/// <summary>
	/// Convert <see cref="DataType"/> to <see cref="ISubscriptionMessage"/> value.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <returns>Subscription message.</returns>
	public static ISubscriptionMessage ToSubscriptionMessage(this DataType dataType)
	{
		if (dataType == null)
			throw new ArgumentNullException(nameof(dataType));

		if (dataType == DataType.Securities)
			return new SecurityLookupMessage();
		else if (dataType == DataType.Board)
			return new BoardLookupMessage();
		else if (dataType == DataType.BoardState)
			return new BoardLookupMessage();
		else if (dataType == DataType.Users)
			return new UserLookupMessage();
		else if (dataType == DataType.DataTypeInfo)
			return new DataTypeLookupMessage();
		else if (dataType.IsMarketData)
			return new MarketDataMessage { DataType2 = dataType };
		else if (dataType == DataType.Transactions)
			return new OrderStatusMessage();
		else if (dataType == DataType.PositionChanges)
			return new PortfolioLookupMessage();
		else if (dataType == DataType.SecurityLegs)
			return new SecurityLegsRequestMessage();
		else if (dataType == DataType.Command)
			return new CommandMessage();
		else
			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);
	}
}