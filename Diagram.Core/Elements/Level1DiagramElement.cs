namespace StockSharp.Diagram.Elements;

/// <summary>
/// The Level1 element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.Level1Key,
	Description = LocalizedStrings.Level1ElementDescriptionKey,
	GroupName = LocalizedStrings.SourcesKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html")]
public class Level1DiagramElement : SubscriptionDiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "44AB379B-F8DC-44D3-A343-0FBB5C3562DA".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "NumberOne";

	private readonly DiagramElementParam<Level1Fields?> _valueType;

	/// <summary>
	/// Level1 field.
	/// </summary>
	public Level1Fields? ValueType
	{
		get => _valueType.Value;
		set => _valueType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Level1DiagramElement"/>.
	/// </summary>
	public Level1DiagramElement()
		: base(LocalizedStrings.Level1)
	{
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Change2, DiagramSocketType.Unit);

		_valueType = AddParam<Level1Fields?>(nameof(ValueType))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Level1, LocalizedStrings.Value, LocalizedStrings.Value, 10)
			.SetOnValueChangedHandler(value =>
			{
				SetElementName(value?.GetDisplayName());
			});
	}

	/// <inheritdoc />
	protected override Subscription OnCreateSubscription(Security security)
	{
		var subscription = new Subscription(DataType.Level1, security);

		subscription
			.WhenLevel1Received(Strategy)
			.Do(l1 =>
			{
				var any = false;

				foreach (var value in l1.Changes)
				{
					if (ValueType != value.Key)
						continue;

					any = true;
					RaiseProcessOutput(_outputSocket, l1.ServerTime, value.Value, null, subscription);
				}

				if (any)
					Strategy.Flush(l1);
			})
			.Apply(Strategy);

		return subscription;
	}
}