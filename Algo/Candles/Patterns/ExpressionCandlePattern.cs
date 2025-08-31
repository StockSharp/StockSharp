namespace StockSharp.Algo.Candles.Patterns;

using System.Text.RegularExpressions;

using Ecng.Compilation;
using Ecng.Compilation.Expressions;

/// <summary>
/// Formula for a single candle inside pattern.
/// </summary>
public class CandleExpressionCondition : IPersistable
{
	/// <summary>
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="Variable"/>.
	/// </remarks>
	/// <param name="varName"><see cref="VarName"/></param>
	/// <param name="description"><see cref="Description"/></param>
	/// <param name="getter"><see cref="PartGetter"/></param>
	public readonly struct Variable(string varName, string description, Func<ICandleMessage, decimal> getter)
	{
		/// <summary>
		/// Name.
		/// </summary>
		public string VarName { get; } = varName;

		/// <summary>
		/// Description.
		/// </summary>
		public string Description { get; } = description;

		/// <summary>
		/// Func to get the candle message part (open, high, low or close).
		/// </summary>
		public Func<ICandleMessage, decimal> PartGetter { get; } = getter;
	}

	private delegate decimal ValueGetterDelegate(ReadOnlySpan<ICandleMessage> candles, int idx);

	private readonly AssemblyLoadContextTracker _context = new();
	private readonly Dictionary<string, ValueGetterDelegate> _varGetters = new(StringComparer.InvariantCultureIgnoreCase);
	private string[] _variables;
	private decimal[] _varValues;
	private ExpressionFormula<bool> _formula;

	/// <summary>
	/// Formula is not present.
	/// </summary>
	public bool IsEmpty => _formula == null;

	/// <summary>
	/// Expression.
	/// </summary>
	public string Expression { get; private set; }

	internal int MinIndex { get; private set; }
	internal int MaxIndex { get; private set; }

	/// <summary>
	/// Create instance.
	/// </summary>
	/// <param name="expression"><see cref="Expression"/></param>
	public CandleExpressionCondition(string expression)
	{
		Expression = expression;
		Init();
	}

	private static readonly Regex _varNamePattern = new(@"^(p+)?([a-zA-Z]+)(\d*)$", RegexOptions.Compiled);

	private void Init()
	{
		_formula = null;
		_varGetters.Clear();

		MinIndex = MaxIndex = 0;

		if(Expression.IsEmptyOrWhiteSpace())
			return;

		if (CodeExtensions.TryGetCSharpCompiler() is null)
			throw new InvalidOperationException(LocalizedStrings.ServiceNotRegistered.Put(nameof(ICompiler)));

		_formula = Expression.Compile<bool>(_context);

		if (!_formula.Error.IsEmpty())
			throw new InvalidOperationException(_formula.Error);

		_variables = [.. _formula.Variables];
		_varValues = new decimal[_variables.Length];

		foreach (var varName in _variables)
		{
			var m = _varNamePattern.Match(varName);
			if(!m.Success)
				throw new InvalidOperationException($"invalid variable '{varName}'");

			var prev = m.Groups[1].Value;
			var name = m.Groups[2].Value;
			var idx = m.Groups[3].Value.IsEmptyOrWhiteSpace() ? 0 : m.Groups[3].Value.To<int>();

			if (!prev.IsEmpty())
			{
				var pLen = prev.Length;

				if (pLen > 1)
				{
					if (idx > 0)
						throw new InvalidOperationException($"cannot use more than 1 'p' prefix with index in variable '{varName}'");

					idx = -pLen;
				}
				else
					idx = -idx - 1;
			}

			MinIndex = MinIndex.Min(idx);
			MaxIndex = MaxIndex.Max(idx);

			var idxVar = Variables.IndexOf(v => v.VarName.EqualsIgnoreCase(name));
			if(idxVar < 0)
				throw new InvalidOperationException($"unknown variable '{varName}'");

			var curVarName = varName;
			_varGetters[varName] = (candles, i) =>
			{
				var actualIndex = i + idx;
				if(actualIndex < 0 || actualIndex >= candles.Length)
					throw new InvalidOperationException($"invalid variable index '{curVarName}' for candle {i} in candles[{candles.Length}] (actual index is {actualIndex})");

				return Variables[idxVar].PartGetter(candles[actualIndex]);
			};
		}
	}

	/// <summary>
	/// Check if the condition is met for the given candles.
	/// </summary>
	/// <param name="candles">The candles to check the condition against.</param>
	/// <param name="candleIndex">The index of the candle in the candles array.</param>
	/// <returns>Check result.</returns>
	public bool CheckCondition(ReadOnlySpan<ICandleMessage> candles, int candleIndex)
	{
		if(_formula == null)
			return true;

		for (var i = 0; i < _variables.Length; ++i)
			_varValues[i] = _varGetters[_variables[i]](candles, candleIndex);

		return _formula.Calculate(_varValues);
	}

	/// <summary>
	/// Candle formula variables.
	/// </summary>
	public static readonly Variable[] Variables =
	[
		new("O",   LocalizedStrings.OpenPrice,      msg => msg.OpenPrice),
		new("H",   LocalizedStrings.HighestPrice,   msg => msg.HighPrice),
		new("L",   LocalizedStrings.LowestPrice,    msg => msg.LowPrice),
		new("C",   LocalizedStrings.ClosingPrice,   msg => msg.ClosePrice),
		new("V",   LocalizedStrings.Volume,         msg => msg.TotalVolume),
		new("OI",  LocalizedStrings.OI,             msg => msg.OpenInterest ?? default),
		new("B",   LocalizedStrings.CandleBody,     msg => msg.GetBody()),
		new("LEN", LocalizedStrings.CandleLength,   msg => msg.GetLength()),
		new("BS",  LocalizedStrings.BottomShadow,   msg => msg.GetBottomShadow()),
		new("TS",  LocalizedStrings.TopShadow,      msg => msg.GetTopShadow()),
	];

	private void EnsureEmpty()
	{
		if(!Expression.IsEmptyOrWhiteSpace())
			throw new InvalidOperationException($"cannot change initialized candle expression (expr='{Expression}')");
	}

	/// <inheritdoc />
	public void Load(SettingsStorage storage)
	{
		EnsureEmpty();

		Expression = storage.GetValue<string>(nameof(Expression));
		Init();
	}

	/// <inheritdoc />
	public void Save(SettingsStorage storage) => storage.SetValue(nameof(Expression), Expression);

	/// <inheritdoc />
	public override string ToString() => Expression.IsEmpty(LocalizedStrings.Empty);
}


/// <summary>
/// Formula based implementation of <see cref="ICandlePattern"/>.
/// </summary>
public class ExpressionCandlePattern : ICandlePattern
{
	/// <summary>
	/// Pattern formulas.
	/// </summary>
	public CandleExpressionCondition[] Conditions { get; private set; }

	/// <inheritdoc />
	public int CandlesCount => Conditions.Length;

	/// <inheritdoc />
	public string Name { get; private set; }

	/// <summary>
	/// Create instance.
	/// </summary>
	public ExpressionCandlePattern() : this(null, []) { }

	/// <summary>
	/// Condition error.
	/// </summary>
	public class ConditionError(string message, IEnumerable<int> indexes) : Exception(message)
	{
		/// <summary>
		/// Indexes of conditions with errors (1-based).
		/// </summary>
		public int[] Indexes { get; } = [.. indexes];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpressionCandlePattern"/>.
	/// </summary>
	/// <param name="name"><see cref="Name"/></param>
	/// <param name="conditions"><see cref="Conditions"/></param>
	public ExpressionCandlePattern(string name, IEnumerable<CandleExpressionCondition> conditions)
	{
		ArgumentNullException.ThrowIfNull(conditions);

		Name = name;
		Conditions = [.. conditions];
	}

	private bool _validated;

	bool ICandlePattern.Recognize(ReadOnlySpan<ICandleMessage> candles)
	{
		if (!_validated)
		{
			_validated = true;

			var invalidRangeIds = new List<int>();

			for (var i = 0; i < Conditions.Length; ++i)
			{
				var cond = Conditions[i];
				if (i + cond.MinIndex < 0 || i + cond.MaxIndex >= Conditions.Length)
					invalidRangeIds.Add(i);
			}

			if (invalidRangeIds.Count > 0)
				throw new ConditionError($"patterns ({invalidRangeIds.Select(i => (i + 1).ToString()).JoinComma()}) use invalid var indexes which go outside of the pattern range", invalidRangeIds);

			if (Conditions.Length == 0)
				throw new InvalidOperationException("no conditions");

			if (Conditions.All(cf => cf.IsEmpty))
				throw new InvalidOperationException("all candle formulas are empty");

			if (CodeExtensions.TryGetCSharpCompiler() is null)
				throw new InvalidOperationException(LocalizedStrings.ServiceNotRegistered.Put(nameof(ICompiler)));
		}

		if(candles.Length != CandlesCount)
			throw new ArgumentException($"unexpected candles count. expected {CandlesCount}, got {candles.Length}");

		for (var i = 0; i < CandlesCount; ++i)
			if(!Conditions[i].CheckCondition(candles, i))
				return false;

		return true;
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		Name = storage.GetValue<string>(nameof(Name));

		Conditions = [.. storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Conditions)).Select(ss =>
		{
			var cond = new CandleExpressionCondition(null);
			cond.Load(ss);
			return cond;
		})];
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Name), Name)
			.Set(nameof(Conditions), Conditions.Select(c => c.Save()).ToArray())
		;
	}

	/// <inheritdoc />
	public override string ToString() => Name;
}
