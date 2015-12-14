#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: RiskPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;

	using StockSharp.Algo.Risk;

	/// <summary>
	/// The panel for the list editing <see cref="IRiskRule"/>.
	/// </summary>
	public partial class RiskPanel
	{
		private class RuleItem
		{
			public string Name { get; set; }
			public IRiskRule Rule { get; set; }
		}

		private readonly Dictionary<Type, string> _names = new Dictionary<Type, string>();
		private readonly ConvertibleObservableCollection<IRiskRule, RuleItem> _rules;

		/// <summary>
		/// Initializes a new instance of the <see cref="RiskPanel"/>.
		/// </summary>
		public RiskPanel()
		{
			InitializeComponent();

			var ruleTypes = new[]
			{
				typeof(RiskCommissionRule),
				typeof(RiskOrderFreqRule),
				typeof(RiskOrderPriceRule),
				typeof(RiskOrderVolumeRule),
				typeof(RiskPnLRule),
				typeof(RiskPositionSizeRule),
				typeof(RiskPositionTimeRule),
				typeof(RiskSlippageRule),
				typeof(RiskTradeFreqRule),
				typeof(RiskTradePriceRule),
				typeof(RiskTradeVolumeRule)
			};

			_names.AddRange(ruleTypes.ToDictionary(t => t, t => t.GetDisplayName()));

			TypeCtrl.ItemsSource = _names;
			TypeCtrl.SelectedIndex = 0;

			var itemsSource = new ObservableCollectionEx<RuleItem>();
			RuleGrid.ItemsSource = itemsSource;

			_rules = new ConvertibleObservableCollection<IRiskRule, RuleItem>(new ThreadSafeObservableCollection<RuleItem>(itemsSource), CreateItem);
		}

		/// <summary>
		/// The list of rules added to the table.
		/// </summary>
		public IListEx<IRiskRule> Rules => _rules;

		private RuleItem CreateItem(IRiskRule rule)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			return new RuleItem { Rule = rule, Name = _names[rule.GetType()] };
		}

		private KeyValuePair<Type, string>? SelectedType => (KeyValuePair<Type, string>?)TypeCtrl.SelectedItem;

		private void AddRule_OnClick(object sender, RoutedEventArgs e)
		{
			var rule = SelectedType.Value.Key.CreateInstance<IRiskRule>();
			_rules.Add(rule);
		}

		private void RemoveRule_OnClick(object sender, RoutedEventArgs e)
		{
			_rules.RemoveRange(RuleGrid.SelectedItems.Cast<RuleItem>().Select(r => r.Rule).ToArray());
		}

		private void RuleGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = (RuleItem)RuleGrid.SelectedItem;

			Settings.SelectedObject = item == null ? null : item.Rule;
			RemoveRule.IsEnabled = item != null;
		}

		private void TypeCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			AddRule.IsEnabled = SelectedType != null;
		}
	}
}