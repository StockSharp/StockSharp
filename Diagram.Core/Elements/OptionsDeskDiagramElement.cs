namespace StockSharp.Diagram.Elements;

/// <summary>
/// Panel for viewing option desk.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OptionDeskKey,
	Description = LocalizedStrings.OptionDeskPanelKey,
	GroupName = LocalizedStrings.OptionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/options/option_desk.html")]
public class OptionsDeskDiagramElement : OptionsBaseModelDiagramElement<BasketBlackScholes>
{
	private IOptionDesk _desk;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "9B60EAA1-BEE4-4AB0-9FFA-FAA117623A60".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Table";

	/// <summary>
	/// Initializes a new instance of the <see cref="OptionsDeskDiagramElement"/>.
	/// </summary>
	public OptionsDeskDiagramElement()
	{
	}

	/// <inheritdoc />
	protected override void ProcessModel(DiagramSocketValue value)
	{
		if (_desk is null)
			return;

		_desk.Model = Model;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_desk = default;
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		_desk = Strategy.GetOptionDesk();

		if (_desk == null)
		{
			LogWarning(LocalizedStrings.NeedToAddOptionDesk);
			return;
		}

		base.OnPrepare();
	}
}