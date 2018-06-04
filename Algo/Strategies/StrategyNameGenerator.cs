#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Algo
File: StrategyNameGenerator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

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
				var value = _values?.TryGetValue(selectorInfo.Selector.Text);

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
		/// Generated or set strategy name.
		/// </summary>
		public string Value
		{
			get => _value ?? (_value = _strategy.Name);
			set
			{
				if (AutoGenerateStrategyName)
					AutoGenerateStrategyName = false;
					//throw new InvalidOperationException("Используется автоматическая генерация имени стратегии. Ручное изменение не допускается.");

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
}