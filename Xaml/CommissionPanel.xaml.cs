#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: CommissionPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
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

	using StockSharp.Algo.Commissions;

	/// <summary>
	/// The panel for the list editing <see cref="ICommissionRule"/>.
	/// </summary>
	public partial class CommissionPanel
	{
		private class RuleItem
		{
			public string Name { get; set; }
			public ICommissionRule Rule { get; set; }
		}

		private readonly Dictionary<Type, string> _names = new Dictionary<Type, string>();
		private readonly ConvertibleObservableCollection<ICommissionRule, RuleItem> _rules;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommissionPanel"/>.
		/// </summary>
		public CommissionPanel()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<RuleItem>();
			RuleGrid.ItemsSource = itemsSource;

			_rules = new ConvertibleObservableCollection<ICommissionRule, RuleItem>(new ThreadSafeObservableCollection<RuleItem>(itemsSource), CreateItem);

			var ruleTypes = new[]
			{
				typeof(CommissionPerOrderCountRule),
				typeof(CommissionPerOrderRule),
				typeof(CommissionPerOrderVolumeRule),
				typeof(CommissionPerTradeCountRule),
				typeof(CommissionPerTradePriceRule),
				typeof(CommissionPerTradeRule),
				typeof(CommissionPerTradeVolumeRule),
				typeof(CommissionSecurityIdRule),
				typeof(CommissionSecurityTypeRule),
				typeof(CommissionTurnOverRule),
				typeof(CommissionBoardCodeRule)
			};

			_names.AddRange(ruleTypes.ToDictionary(t => t, t => t.GetDisplayName()));

			TypeCtrl.ItemsSource = _names;
			TypeCtrl.SelectedIndex = 0;
		}

		/// <summary>
		/// The list of commission rules added to the table.
		/// </summary>
		public IListEx<ICommissionRule> Rules => _rules;

		private RuleItem CreateItem(ICommissionRule rule)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			return new RuleItem { Rule = rule, Name = _names[rule.GetType()] };
		}

		private KeyValuePair<Type, string>? SelectedType => (KeyValuePair<Type, string>?)TypeCtrl.SelectedItem;

		private void AddRule_OnClick(object sender, RoutedEventArgs e)
		{
			var rule = SelectedType.Value.Key.CreateInstance<ICommissionRule>();
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