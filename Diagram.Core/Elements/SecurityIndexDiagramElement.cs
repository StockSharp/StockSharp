namespace StockSharp.Diagram.Elements;

using StockSharp.Algo.Expressions;

/// <summary>
/// Security index based on <see cref="ExpressionIndexSecurity"/> diagram element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.IndexKey,
	Description = LocalizedStrings.IndexSecurityKey,
	GroupName = LocalizedStrings.SourcesKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/data_sources/index.html")]
public class SecurityIndexDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	private ExpressionIndexSecurity _indexSecurity;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "D9F4DD6F-2676-4A3F-A279-1E7ACFD44117".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Chart3";

	private readonly DiagramElementParam<string> _index;

	/// <summary>
	/// Index.
	/// </summary>
	public string Index
	{
		get => _index.Value;
		set => _index.Value = value;
	}

	private readonly DiagramElementParam<bool> _ignoreErrors;

	/// <summary>
	/// Ignore calculation errors (like arithmetic overflows).
	/// </summary>
	public bool IgnoreErrors
	{
		get => _ignoreErrors.Value;
		set => _ignoreErrors.Value = value;
	}

	private readonly DiagramElementParam<bool> _calculateExtended;

	/// <summary>
	/// Calculate extended information.
	/// </summary>
	public bool CalculateExtended
	{
		get => _calculateExtended.Value;
		set => _calculateExtended.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityIndexDiagramElement"/>.
	/// </summary>
	public SecurityIndexDiagramElement()
	{
		_outputSocket = AddOutput(StaticSocketIds.Security, LocalizedStrings.Security, DiagramSocketType.Security);

		_index = AddParam<string>(nameof(Index))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Index, LocalizedStrings.Index, LocalizedStrings.Formula, 10);

		_ignoreErrors = AddParam<bool>(nameof(IgnoreErrors))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Index, LocalizedStrings.IgnoreErrors, LocalizedStrings.IgnoreErrorsDesc, 20);

		_calculateExtended = AddParam<bool>(nameof(CalculateExtended))
			.SetDisplay(LocalizedStrings.Index, LocalizedStrings.ExtendedInfo, LocalizedStrings.CalculateExtended, 30);
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		_indexSecurity = new()
		{
			Expression = Index,
			IgnoreErrors = IgnoreErrors,
			CalculateExtended = CalculateExtended,
			Board = ExchangeBoard.Associated
		};

		if (!_indexSecurity.Formula.Error.IsEmpty())
			throw new InvalidOperationException(_indexSecurity.Formula.Error);

		var set = _indexSecurity
			.InnerSecurityIds
			.Select(id => id)
			.Distinct();

		var notFoundSecurities = _indexSecurity.Formula.Variables.Where(v => !set.Contains(v.ToSecurityId())).ToArray();

		if (notFoundSecurities.Length > 0)
			throw new InvalidOperationException(LocalizedStrings.SecuritiesNotFound.Put(notFoundSecurities.JoinCommaSpace()));

		foreach (var id in set)
		{
			Strategy.Subscribe(new(new SecurityLookupMessage { SecurityId = id }));
		}

		RaiseProcessOutput(_outputSocket, time, _indexSecurity);

		base.OnStart(time);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_indexSecurity = default;
	}
}