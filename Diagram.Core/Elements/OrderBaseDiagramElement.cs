namespace StockSharp.Diagram.Elements;

/// <summary>
/// Order base element.
/// </summary>
public abstract class OrderBaseDiagramElement : DiagramElement
{
	private readonly DiagramElementParam<bool> _onlineOnly;

	/// <summary>
	/// Allow transactions only when strategy is online.
	/// </summary>
	public bool OnlineOnly
	{
		get => _onlineOnly.Value;
		set => _onlineOnly.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBaseDiagramElement"/>.
	/// </summary>
	protected OrderBaseDiagramElement()
	{
		TriggerSocket = AddInput(StaticSocketIds.Trigger, LocalizedStrings.Trigger, DiagramSocketType.Any, linkableMax: int.MaxValue);
		_onlineOnly = AddParam(nameof(OnlineOnly), true).SetDisplay(LocalizedStrings.Order, LocalizedStrings.OnlineOnly, LocalizedStrings.OnlineOnlyDescription, 1000);
	}

	/// <summary>
	/// Trigger socket.
	/// </summary>
	protected DiagramSocket TriggerSocket { get; }

	/// <summary>
	/// Can process order action.
	/// </summary>
	/// <param name="values">Values.</param>
	/// <returns>Check result.</returns>
	protected bool CanProcess(IDictionary<DiagramSocket, DiagramSocketValue> values)
	{
		if (!values.TryGetValue(TriggerSocket, out var triggerValue))
		{
			LogDebug("no trigger");
			return false;
		}

		return CanProcess(triggerValue);
	}

	/// <summary>
	/// Can process order action.
	/// </summary>
	/// <param name="triggerValue"><see cref="TriggerSocket"/> value.</param>
	/// <returns>Check result.</returns>
	protected bool CanProcess(DiagramSocketValue triggerValue)
	{
		if (triggerValue is null)
			throw new ArgumentNullException(nameof(triggerValue));

		if (triggerValue.GetValue<bool?>() == false)
			return false;

		if (ProcessingLevel > 1)
			return false;

		if (OnlineOnly && !Strategy.IsOnline)
		{
			LogVerbose("strategy is not online, transaction is not allowed.");
			return false;
		}

		return true;
	}
}
