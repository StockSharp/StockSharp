namespace StockSharp.Algo.Strategies;

using SmartFormat;
using SmartFormat.Core.Extensions;
using SmartFormat.Core.Parsing;

/// <summary>
/// The class for the strategy name formation.
/// </summary>
public sealed class StrategyNameGenerator
{
	private sealed class Source : ISource
	{
		private readonly Dictionary<string, string> _values;

		public Source(SmartFormatter formatter, Dictionary<string, string> values)
		{
			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));

			if (values == null)
				throw new ArgumentNullException(nameof(values));

			formatter.Parser.AddAlphanumericSelectors();
			formatter.Parser.AddAdditionalSelectorChars("_");
			formatter.Parser.AddOperators(".");

			_values = values;
		}

		bool ISource.TryEvaluateSelector(ISelectorInfo selectorInfo)
		{
			var value = _values?.TryGetValue(selectorInfo.SelectorText);

			if (value == null)
				return false;

			selectorInfo.Result = value;
			return true;
		}
	}

	private readonly SmartFormatter _formatter;
	private readonly Strategy _strategy;
	private readonly SynchronizedSet<string> _selectors;
	private string _value;
	private string _pattern;

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyNameGenerator"/>.
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	public StrategyNameGenerator(Strategy strategy)
	{
		_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
		_strategy.PropertyChanged += (s, e) =>
		{
			if (_selectors.Contains(e.PropertyName))
				Refresh();
		};

		ShortName = new string([.. _strategy.GetType().Name.Where(char.IsUpper)]);

		_formatter = Smart.CreateDefaultSmartFormat();
		_formatter.SourceExtensions.Add(new Source(_formatter, new Dictionary<string, string>
		{
			{ "FullName", _strategy.GetType().Name },
			{ nameof(ShortName), ShortName },
		}));

		_selectors = [];

		AutoGenerateStrategyName = true;
		Pattern = "{ShortName}{Security:_{0.Security}|}{Portfolio:_{0.Portfolio}|}";
	}

	/// <summary>
	/// The name change event.
	/// </summary>
	public event Action<string> Changed;

	/// <summary>
	/// Whether to use the automatic generation of the strategy name. It is enabled by default.
	/// </summary>
	public bool AutoGenerateStrategyName { get; set; }

	/// <summary>
	/// The strategy brief name.
	/// </summary>
	public string ShortName { get; }

	/// <summary>
	/// The pattern for strategy name formation.
	/// </summary>
	public string Pattern
	{
		get => _pattern;
		set
		{
			if (_pattern == value)
				return;

			_pattern = value;

			var format = _formatter.Parser.ParseFormat(value, []);
			var selectors = format
				.Items
				.OfType<Placeholder>()
				.SelectMany(ph => ph.Selectors)
				.Select(s => s.RawText)
				.Distinct();

			_selectors.Clear();
			_selectors.AddRange(selectors);

			Refresh();
		}
	}

	/// <summary>
	/// Generated or set strategy name.
	/// </summary>
	public string Value
	{
		get => _value ??= _strategy.Name;
		set
		{
			if (AutoGenerateStrategyName)
				AutoGenerateStrategyName = false;

			_value = value;
			Changed?.Invoke(_value);
		}
	}

	private void Refresh()
	{
		if (!AutoGenerateStrategyName)
			return;

		_value = _formatter.Format(Pattern, _strategy);
		Changed?.Invoke(_value);
	}
}