namespace StockSharp.Diagram.Elements;

using System.Text.RegularExpressions;

using SmartFormat;
using SmartFormat.Core.Formatting;

/// <summary>
/// String concatenation and formatting element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.StringConcatKey,
	Description = LocalizedStrings.StringConcatDescKey,
	GroupName = LocalizedStrings.InformKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/notifying/string_concat.html")]
public sealed class StringConcatDiagramElement : DiagramElement
{
	/// <inheritdoc />
	public override Guid TypeId { get; } = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Add";

	private readonly DiagramSocket _outputSocket;
	private readonly DiagramElementParam<string> _template;

	private FormatCache _templateCache;
	private readonly HashSet<string> _templateVariables = [];

	/// <summary>
	/// String concatenation and formatting template.
	/// </summary>
	public string Template
	{
		get => _template.Value;
		set => _template.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StringConcatDiagramElement"/>.
	/// </summary>
	public StringConcatDiagramElement()
	{
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Text, DiagramSocketType.String);

		_template = AddParam(nameof(Template), string.Empty)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Common, LocalizedStrings.Template, LocalizedStrings.StringConcatFormatTemplate, 10)
			.SetOnValueChangedHandler(value =>
			{
				_templateCache = null;
				SetElementName(value.Truncate(20).IsEmpty(GetDisplayName()));

				UpdateSocketsFromTemplate(value);
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

		_templateCache = null;
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		var template = Template;

		// Create data object with all input values
		var data = new Dictionary<string, object>();

		foreach (var (s, v) in values)
		{
			var socketName = s.Name;
			var socketValue = v.Value;

			data[socketName] = socketValue;
		}

		var formattedText = Smart.Default.FormatWithCache(ref _templateCache, template, data);

		RaiseProcessOutput(_outputSocket, time, formattedText, source);
	}

	private void UpdateSocketsFromTemplate(string template)
	{
		if (template.IsEmpty())
		{
			// Remove all dynamic sockets
			foreach (var socket in InputSockets.ToArray())
			{
				RemoveSocket(socket);
			}

			_templateVariables.Clear();
			return;
		}

		var variables = ExtractVariableNames(template);
		var actualSocketIds = new HashSet<string>();

		// Create sockets for each variable
		foreach (var variable in variables)
		{
			var socketId = GenerateSocketId(variable);
			actualSocketIds.Add(socketId);

			if (InputSockets.FirstOrDefault(s => s.Id == socketId) != null)
				continue;

			AddInput(socketId, variable, DiagramSocketType.Any);
		}

		// Remove sockets for variables that are no longer in the template
		var socketsToRemove = InputSockets
			.Where(s => !actualSocketIds.Contains(s.Id))
			.ToArray();

		foreach (var socket in socketsToRemove)
		{
			RemoveSocket(socket);
		}

		_templateVariables.Clear();
		_templateVariables.AddRange(variables);
	}

	private static IEnumerable<string> ExtractVariableNames(string template)
	{
		var variables = new HashSet<string>();

		if (template.IsEmpty())
			return variables;

		// Simple regex to find placeholders like {variable}, {variable:format}, {variable.property}
		var regex = new Regex(@"\{([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*)", RegexOptions.Compiled);
		var matches = regex.Matches(template);

		foreach (Match match in matches)
		{
			var variable = match.Groups[1].Value;
			
			// Extract the root variable name (before any dots for property access)
			var rootVariable = variable.Contains('.') ? variable.Split('.')[0] : variable;
			
			variables.Add(rootVariable);
		}

		return [.. variables];
	}
}