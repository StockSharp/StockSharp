using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Compilation;
using Ecng.Compilation.Expressions;
using Ecng.Serialization;

using StockSharp.Localization;
using StockSharp.Messages;

namespace StockSharp.Algo.Candles.Patterns;

/// <summary>
/// Formula for a single candle inside pattern.
/// </summary>
public class CandleExpressionCondition : IPersistable
{
	/// <summary>
	/// </summary>
	public readonly struct Variable
	{
		/// <summary>
		/// Name.
		/// </summary>
		public string VarName { get; }

		/// <summary>
		/// Description.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Func to get the candle message part (open, high, low or close).
		/// </summary>
		public Func<ICandleMessage, decimal> PartGetter { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Variable"/>.
		/// </summary>
		/// <param name="varName"><see cref="VarName"/></param>
		/// <param name="description"><see cref="Description"/></param>
		/// <param name="getter"><see cref="PartGetter"/></param>
		public Variable(string varName, string description, Func<ICandleMessage, decimal> getter)
		{
			VarName = varName;
			Description = description;
			PartGetter = getter;
		}
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

	private static readonly Regex _varNamePattern = new(@"^([a-zA-Z]+)(\d*)$", RegexOptions.Compiled);

	private void Init()
	{
		_formula = null;
		_varGetters.Clear();

		MinIndex = MaxIndex = 0;

		if(Expression.IsEmptyOrWhiteSpace())
			return;

		if (ServicesRegistry.TryCompiler is null)
			throw new InvalidOperationException(LocalizedStrings.ServiceNotRegistered.Put(nameof(ICompiler)));

		_formula = Expression.Compile<bool>(_context);

		if (!_formula.Error.IsEmpty())
			throw new InvalidOperationException(_formula.Error);

		_variables = _formula.Variables.ToArray();
		_varValues = new decimal[_variables.Length];

		foreach (var varName in _variables)
		{
			var m = _varNamePattern.Match(varName);
			if(!m.Success)
				throw new InvalidOperationException($"invalid variable '{varName}'");

			var name = m.Groups[1].Value;
			var idx = m.Groups[2].Value.IsEmptyOrWhiteSpace() ? 0 : m.Groups[2].Value.To<int>();

			if(name.StartsWithIgnoreCase("p"))
			{
				idx = -idx - 1;
				name = name.Substring(1);
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
	/// </summary>
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
	{
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
	};

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

	internal bool IsRegistry { get; init; }

	/// <summary>
	/// Create instance.
	/// </summary>
	public ExpressionCandlePattern() : this(null, Enumerable.Empty<CandleExpressionCondition>()) { }

	/// <summary>
	/// Condition error.
	/// </summary>
	public class ConditionError : Exception
	{
		/// <summary>
		/// </summary>
		public int[] Indexes { get; }

		/// <summary>
		/// </summary>
		public ConditionError(string message, IEnumerable<int> indexes) : base(message) => Indexes = indexes.ToArray();
	}

	/// <summary>
	/// Create instance.
	/// </summary>
	/// <param name="name"><see cref="Name"/></param>
	/// <param name="conditions"><see cref="Conditions"/></param>
	public ExpressionCandlePattern(string name, IEnumerable<CandleExpressionCondition> conditions)
	{
		Conditions = conditions?.ToArray() ?? throw new ArgumentNullException(nameof(conditions));

		if(Conditions.IsEmpty())
			return; // for Load to work

		Name = name;

		var invalidRangeIds = new List<int>();

		for (var i = 0; i < Conditions.Length; ++i)
		{
			var cond = Conditions[i];
			if(i + cond.MinIndex < 0 || i + cond.MaxIndex >= Conditions.Length)
				invalidRangeIds.Add(i);
		}

		if(invalidRangeIds.Count > 0)
			throw new ConditionError($"patterns ({invalidRangeIds.Select(i => (i+1).ToString()).JoinComma()}) use invalid var indexes which go outside of the pattern range", invalidRangeIds);

		if(Conditions.All(cf => cf.IsEmpty))
			throw new InvalidOperationException("all candle formulas are empty");

		if (ServicesRegistry.TryCompiler is null)
			throw new InvalidOperationException(LocalizedStrings.ServiceNotRegistered.Put(nameof(ICompiler)));
	}

	bool ICandlePattern.Recognize(ReadOnlySpan<ICandleMessage> candles)
	{
		if(Conditions.Length == 0)
			throw new InvalidOperationException("no conditions");

		if(candles.Length != CandlesCount)
			throw new ArgumentException($"unexpected candles count. expected {CandlesCount}, got {candles.Length}");

		for (var i = 0; i < CandlesCount; ++i)
			if(!Conditions[i].CheckCondition(candles, i))
				return false;

		return true;
	}

	private void EnsureEmpty()
	{
		if(Name != null || Conditions.Length > 0)
			throw new InvalidOperationException($"cannot change initialized pattern (name='{Name}', {Conditions.Length} conditions)");
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		EnsureEmpty();

		Name = storage.GetValue<string>(nameof(Name));

		Conditions = storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Conditions)).Select(ss =>
		{
			var cond = new CandleExpressionCondition(null);
			cond.Load(ss);
			return cond;
		}).ToArray();
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.Set(nameof(Name), Name);
		storage.Set(nameof(Conditions), Conditions.Select(c => c.Save()).ToArray());
	}

	/// <inheritdoc />
	public override string ToString() => Name;
}
