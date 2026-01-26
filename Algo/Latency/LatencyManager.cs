namespace StockSharp.Algo.Latency;

/// <summary>
/// Orders registration delay calculation manager.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LatencyManager"/>.
/// </remarks>
/// <param name="state">State storage.</param>
public class LatencyManager(ILatencyManagerState state) : ILatencyManager
{
	private readonly ILatencyManagerState _state = state ?? throw new ArgumentNullException(nameof(state));

	/// <inheritdoc />
	public TimeSpan LatencyRegistration => _state.LatencyRegistration;

	/// <inheritdoc />
	public TimeSpan LatencyCancellation => _state.LatencyCancellation;

	/// <inheritdoc />
	public TimeSpan? ProcessMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				Reset();
				break;
			}

			case MessageTypes.OrderRegister:
			{
				var regMsg = (OrderRegisterMessage)message;
				_state.AddRegistration(regMsg.TransactionId, regMsg.LocalTime);
				break;
			}

			case MessageTypes.OrderReplace:
			{
				var replaceMsg = (OrderReplaceMessage)message;
				_state.AddCancellation(replaceMsg.TransactionId, replaceMsg.LocalTime);
				_state.AddRegistration(replaceMsg.TransactionId, replaceMsg.LocalTime);
				break;
			}

			case MessageTypes.OrderCancel:
			{
				var cancelMsg = (OrderCancelMessage)message;
				_state.AddCancellation(cancelMsg.TransactionId, cancelMsg.LocalTime);
				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (!execMsg.HasOrderInfo())
					break;

				if (execMsg.OrderState == OrderStates.Pending)
					break;

				var transId = execMsg.OriginalTransactionId;

				if (!_state.TryGetAndRemoveRegistration(transId, out var time))
				{
					if (_state.TryGetAndRemoveCancellation(transId, out time))
					{
						if (execMsg.OrderState == OrderStates.Failed)
							break;

						var latency = execMsg.LocalTime - time;
						_state.AddLatencyCancellation(latency);
						return latency;
					}
				}
				else
				{
					if (execMsg.OrderState == OrderStates.Failed)
						break;

					var latency = execMsg.LocalTime - time;
					_state.AddLatencyRegistration(latency);
					return latency;
				}

				break;
			}
		}

		return null;
	}

	/// <inheritdoc />
	public void Reset()
	{
		_state.Clear();
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public void Load(SettingsStorage storage)
	{
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public void Save(SettingsStorage storage)
	{
	}
}
