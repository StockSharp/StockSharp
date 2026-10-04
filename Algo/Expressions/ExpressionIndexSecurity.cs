namespace StockSharp.Algo.Expressions;

using Ecng.Compilation.Expressions;
using Ecng.Compilation;

/// <summary>
/// The index, built of combination of several instruments through mathematical formula <see cref="Expression"/>.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.IndexKey,
	Description = LocalizedStrings.IndexSecurityKey)]
[BasketCode(BasketCodes.ExpressionIndex)]
public class ExpressionIndexSecurity : IndexSecurity, IDisposable
{
	private readonly AssemblyLoadContextTracker _context = new();
	private readonly AsyncLock _compileLock = new();
	private readonly Lock _sync = new();

	void IDisposable.Dispose()
	{
		_context.Dispose();

		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpressionIndexSecurity"/>.
	/// </summary>
	public ExpressionIndexSecurity()
	{
	}

	// The formula compiled from the current text, or null while that text has not been compiled yet.
	private volatile ExpressionFormula<decimal> _formula = ExpressionFormula<decimal>.CreateError(LocalizedStrings.ExpressionNotSet);
	private string _expression;

	/// <summary>
	/// Compiled mathematical formula.
	/// </summary>
	[Obsolete("Blocking sync-over-async wrapper. Use GetFormulaAsync instead.")]
	public ExpressionFormula<decimal> Formula => _formula ?? AsyncHelper.Run(() => GetFormulaAsync(default));

	/// <summary>
	/// Get the mathematical formula compiled from <see cref="Expression"/>.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Compiled mathematical formula. A text that cannot be compiled gives a formula whose <see cref="ExpressionFormula{TResult}.Error"/> says why.</returns>
	public async ValueTask<ExpressionFormula<decimal>> GetFormulaAsync(CancellationToken cancellationToken)
	{
		var formula = _formula;

		if (formula is not null)
			return formula;

		using (await _compileLock.LockAsync(cancellationToken))
		{
			string expression;

			using (_sync.EnterScope())
			{
				formula = _formula;
				expression = _expression;
			}

			if (formula is not null)
				return formula;

			formula = CodeExtensions.TryGetCSharpCompiler() is null
				? ExpressionFormula<decimal>.CreateError(LocalizedStrings.ServiceNotRegistered.Put(nameof(ICompiler)))
				: await expression.CompileAsync(Paths.FileSystem, _context, cancellationToken);

			if (!formula.Error.IsEmpty())
				new InvalidOperationException(formula.Error).LogError();

			using (_sync.EnterScope())
			{
				// the text may have been replaced while it was being compiled
				if (_expression == expression)
					_formula = formula;
			}

			return formula;
		}
	}

	/// <summary>
	/// The mathematical formula of index.
	/// </summary>
	/// <remarks>
	/// The instruments the text names are known at once (<see cref="InnerSecurityIds"/>); the text is compiled
	/// by <see cref="GetFormulaAsync"/>.
	/// </remarks>
	[Browsable(false)]
	public string Expression
	{
		get => _expression;
		set
		{
			var ids = new List<SecurityId>();
			List<Exception> errors = null;

			if (!value.IsEmpty())
			{
				foreach (var v in GetVariables(value))
				{
					try
					{
						ids.Add(v.ToSecurityId());
					}
					catch (Exception ex)
					{
						errors ??= [];
						errors.Add(ex);
					}
				}
			}

			using (_sync.EnterScope())
			{
				_expression = value;
				_formula = value.IsEmpty() ? ExpressionFormula<decimal>.CreateError(LocalizedStrings.ExpressionNotSet) : null;

				_innerSecurityIds.Clear();
				_innerSecurityIds.AddRange(ids);
			}

			if (errors is not null)
				throw new AggregateException(LocalizedStrings.InvalidValue, errors);
		}
	}

	// A text that cannot be read names no instruments; what is wrong with it is said by the compiler.
	private static string[] GetVariables(string expression)
	{
		try
		{
			return ExpressionHelper.GetVariables(expression);
		}
		catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
		{
			return [];
		}
	}

	private readonly CachedSynchronizedList<SecurityId> _innerSecurityIds = [];

	/// <inheritdoc />
	public override IEnumerable<SecurityId> InnerSecurityIds => _innerSecurityIds.Cache;

	/// <inheritdoc />
	public override Security Clone()
	{
		var clone = new ExpressionIndexSecurity { Expression = Expression };
		CopyTo(clone);
		return clone;
	}

	/// <inheritdoc />
	public override string ToString() => Expression;

	/// <inheritdoc />
	protected override string ToSerializedString()
	{
		return Expression;
	}

	/// <inheritdoc />
	protected override void FromSerializedString(string text)
	{
		Expression = text;
	}
}
