namespace StockSharp.Diagram.Elements;

using SmartFormat;
using SmartFormat.Core.Parsing;

/// <summary>
/// String formatting element for single value using templates.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.StringFormatterKey,
	Description = LocalizedStrings.StringFormatterDescKey,
	GroupName = LocalizedStrings.InformKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html")]
public sealed class StringFormatDiagramElement : DiagramElement
{
	private static readonly SmartFormatter _formatter = StringHelper.CreateSmartFormatterEx();

	/// <inheritdoc />
	public override Guid TypeId { get; } = "96387CFD-3180-4575-9C00-DF214B503D41".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "CurlyBrackets";

	private Format _parsedFormat;

	private readonly DiagramSocket _outputSocket;
	private readonly DiagramElementParam<string> _template;

	/// <summary>
	/// String formatting template using for single value.
	/// </summary>
	public string Template
	{
		get => _template.Value;
		set => _template.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StringFormatDiagramElement"/>.
	/// </summary>
	public StringFormatDiagramElement()
	{
		AddInput(StaticSocketIds.Input, LocalizedStrings.Input, DiagramSocketType.Any, OnProcess, int.MaxValue);
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Text, DiagramSocketType.String);

		_template = AddParam(nameof(Template), "{0}")
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Common, LocalizedStrings.Template, LocalizedStrings.StringFormatTemplate, 10)
			.SetOnValueChangedHandler(value =>
			{
				_parsedFormat = null;
				SetElementName(value.Truncate(20).IsEmpty(GetDisplayName()));
			});

		SetElementName(GetDisplayName());
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		if (Template.IsEmpty())
			throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Template));

		base.OnPrepare();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_parsedFormat = null;
	}

	private void OnProcess(DiagramSocketValue value)
	{
		var template = Template;
		var inputValue = value.Value;

		_parsedFormat ??= _formatter.Parser.ParseFormat(template);
		var formattedText = _formatter.Format(_parsedFormat, inputValue);
		RaiseProcessOutput(_outputSocket, value.Time, formattedText, value);
	}
}