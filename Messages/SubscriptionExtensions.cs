namespace StockSharp.Messages;

using System;

using StockSharp.Localization;
using StockSharp.Logging;

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
	/// <param name="isInfoLevel">Use <see cref="LogLevels.Info"/> for log message.</param>
	/// <returns>New state.</returns>
	public static SubscriptionStates ChangeSubscriptionState(this SubscriptionStates currState, SubscriptionStates newState, long subscriptionId, ILogReceiver receiver, bool isInfoLevel = true)
	{
		bool isOk;

		if (currState == newState)
			isOk = false;
		else
		{
			switch (currState)
			{
				case SubscriptionStates.Stopped:
				case SubscriptionStates.Active:
					isOk = true;
					break;
				case SubscriptionStates.Error:
				case SubscriptionStates.Finished:
					isOk = false;
					break;
				case SubscriptionStates.Online:
					isOk = newState != SubscriptionStates.Active;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(currState), currState, LocalizedStrings.Str1219);
			}
		}

		const string text = "Subscription {0} {1}->{2}.";

		if (isOk)
		{
			if (isInfoLevel)
				receiver.AddInfoLog(text, subscriptionId, currState, newState);
			else
				receiver.AddDebugLog(text, subscriptionId, currState, newState);
		}
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
		else if (dataType == DataType.TimeFrames)
			return new TimeFrameLookupMessage();
		else if (dataType.IsMarketData)
			return new MarketDataMessage { DataType2 = dataType };
		else if (dataType == DataType.Transactions)
			return new OrderStatusMessage();
		else if (dataType == DataType.PositionChanges)
			return new PortfolioLookupMessage();
		else if (dataType.IsPortfolio)
			return new PortfolioMessage();
		else if (dataType == DataType.SecurityLegs)
			return new SecurityLegsRequestMessage();
		else if (dataType == DataType.SecurityMapping)
			return new SecurityMappingRequestMessage();
		else if (dataType == DataType.SecurityRoute)
			return new SecurityRouteListRequestMessage();
		else if (dataType == DataType.PortfolioRoute)
			return new PortfolioRouteListRequestMessage();
		else if (dataType == DataType.Command)
			return new CommandMessage();
		else
			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1219);
	}
}