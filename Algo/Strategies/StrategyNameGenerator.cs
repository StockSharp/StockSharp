namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using SmartFormat;
	using SmartFormat.Core.Extensions;
	using SmartFormat.Core.Parsing;

	/// <summary>
	/// Класс для формирования имени стратегии.
	/// </summary>
	public sealed class StrategyNameGenerator
	{
		private sealed class Source : ISource
		{
			private readonly Dictionary<string, string> _values;

			public Source(SmartFormatter formatter, Dictionary<string, string> values)
			{
				if (formatter == null)
					throw new ArgumentNullException("formatter");

				if (values == null)
					throw new ArgumentNullException("values");

				formatter.Parser.AddAlphanumericSelectors();
				formatter.Parser.AddAdditionalSelectorChars("_");
				formatter.Parser.AddOperators(".");

				_values = values;
			}

			public void EvaluateSelector(object current, Selector selector, ref bool handled, ref object result, FormatDetails formatDetails)
			{
				if (_values == null)
					return;

				var value = _values.TryGetValue(selector.Text);

				if (value == null)
					return;

				result = value;
				handled = true;
			}
		}

		private readonly SmartFormatter _formatter;
		private readonly Strategy _strategy;
		private readonly SynchronizedSet<string> _selectors;
		private string _value;
		private string _pattern;

		/// <summary>
		/// Создать <see cref="StrategyNameGenerator"/>.
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		public StrategyNameGenerator(Strategy strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			_strategy = strategy;
			_strategy.SecurityChanged += () =>
			{
				if (_selectors.Contains("Security"))
					Refresh();
			};
			_strategy.PortfolioChanged += () =>
			{
				if (_selectors.Contains("Portfolio"))
					Refresh();
			};

			ShortName = new string(_strategy.GetType().Name.Where(char.IsUpper).ToArray());

			_formatter = Smart.CreateDefaultSmartFormat();
			_formatter.SourceExtensions.Add(new Source(_formatter, new Dictionary<string, string>
			{
				{ "FullName", _strategy.GetType().Name },
				{ "ShortName", ShortName },
			}));

			_selectors = new SynchronizedSet<string>();

			AutoGenerateStrategyName = true;
			Pattern = "{ShortName}{Security:_{0.Security}|}{Portfolio:_{0.Portfolio}|}";
		}

		/// <summary>
		/// Событие изменения имени.
		/// </summary>
		public event Action<string> Changed;

		/// <summary>
		/// Использовать ли автоматическую генерацию имени стратегии. По-умолчанию включено.
		/// </summary>
		public bool AutoGenerateStrategyName { get; set; }

		/// <summary>
		/// Короткое название стратегии.
		/// </summary>
		public string ShortName { get; private set; }

		/// <summary>
		/// Паттерн для формирования имени стратегии.
		/// </summary>
		public string Pattern
		{
			get { return _pattern; }
			set
			{
				if (_pattern == value)
					return;

				_pattern = value;

				var format = _formatter.Parser.ParseFormat(value);
				var selectors = format
					.Items
					.OfType<Placeholder>()
					.SelectMany(ph => ph.Selectors)
					.Select(s => s.Text)
					.Distinct();

				_selectors.Clear();
				_selectors.AddRange(selectors);

				Refresh();
			}
		}

		/// <summary>
		/// Сгенерированное или установленное имя стратегии.
		/// </summary>
		public string Value
		{
			get { return _value ?? (_value = _strategy.Name); }
			set
			{
				if (AutoGenerateStrategyName)
					AutoGenerateStrategyName = false;
					//throw new InvalidOperationException("Используется автоматическая генерация имени стратегии. Ручное изменение не допускается.");

				_value = value;
				Changed.SafeInvoke(_value);
			}
		}

		private void Refresh()
		{
			if (!AutoGenerateStrategyName)
				return;

			_value = _formatter.Format(Pattern, _strategy);
			Changed.SafeInvoke(_value);
		}
	}
}