#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alerts.Alerts
File: AlertSettingsPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Alerts
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Reflection;
	using StockSharp.Localization;

	/// <summary>
	/// Panel schema parameter modification.
	/// </summary>
	public partial class AlertSettingsPanel
	{
		private static readonly Tuple<string, PropertyInfo> _nullField = new Tuple<string, PropertyInfo>(LocalizedStrings.Select, null);
		private readonly ObservableCollection<ComparisonOperator> _operators = new ObservableCollection<ComparisonOperator>();
		private readonly ObservableCollection<Tuple<string, PropertyInfo>> _fields;
		private static readonly HashSet<string> _ignoringFields = new HashSet<string>
		{
			"ExtensionInfo", "Type", "OriginalTransactionId"
		}; 

		/// <summary>
		/// Initializes a new instance of the <see cref="AlertSettingsPanel"/>.
		/// </summary>
		public AlertSettingsPanel()
		{
			InitializeComponent();

			OperatorCtrl.ItemsSource = _operators;

			_fields = new ObservableCollection<Tuple<string, PropertyInfo>> { _nullField };
			PropertyCtrl.ItemsSource = _fields;
			PropertyCtrl.SelectedValue = _nullField;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="AlertSettingsPanel.MessageType"/>.
		/// </summary>
		public static readonly DependencyProperty MessageTypeProperty =
			DependencyProperty.Register(nameof(MessageType), typeof(Type), typeof(AlertSettingsPanel), new PropertyMetadata(null, MessageTypeChanged));

		private static void MessageTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((AlertSettingsPanel)d).MessageType = (Type)e.NewValue;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="AlertSettingsPanel.Property"/>.
		/// </summary>
		public static readonly DependencyProperty PropertyProperty =
			DependencyProperty.Register(nameof(Property), typeof(PropertyInfo), typeof(AlertSettingsPanel), new PropertyMetadata(null, PropertyChanged));

		private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = (AlertSettingsPanel)d;
			var prop = (PropertyInfo)e.NewValue;

			if (ctrl.MessageType == null)
				ctrl.MessageType = prop.ReflectedType;

			ctrl.PropertyCtrl.SelectedValue = prop == null ? _nullField : ctrl._fields.First(t => t.Item2 == prop);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="AlertSettingsPanel.Operator"/>.
		/// </summary>
		public static readonly DependencyProperty OperatorProperty =
			DependencyProperty.Register(nameof(Operator), typeof(ComparisonOperator?), typeof(AlertSettingsPanel), new PropertyMetadata(null, OperatorChanged));

		private static void OperatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((AlertSettingsPanel)d).OperatorCtrl.SelectedValue = (ComparisonOperator?)e.NewValue;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Value"/>.
		/// </summary>
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(nameof(Value), typeof(object), typeof(AlertSettingsPanel), new PropertyMetadata(null, ValueChanged));

		private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = (AlertSettingsPanel)d;
			var value = e.NewValue;

			if (ctrl.DecimalValue.Visibility == Visibility.Visible)
				ctrl.DecimalValue.Value = (decimal?)value;
			else if (ctrl.TextValue.Visibility == Visibility.Visible)
				ctrl.TextValue.Text = (string)value;
			else if (ctrl.TimeValue.Visibility == Visibility.Visible)
				ctrl.TimeValue.Value = DateTime.Today + (TimeSpan?)value;
			else if (ctrl.DateValue.Visibility == Visibility.Visible)
				ctrl.DateValue.Value = (DateTime?)value;
			//else if (ctrl.SecurityValue.Visibility == Visibility.Visible)
			//	ctrl.SecurityValue.SelectedSecurity = (Security)value;
			//else if (ctrl.PortfolioValue.Visibility == Visibility.Visible)
			//	ctrl.PortfolioValue.SelectedPortfolio = (Portfolio)value;
			else if (ctrl.EnumValue.Visibility == Visibility.Visible)
				ctrl.EnumValue.SelectedValue = value;
		}

		private Type _messageType;

		/// <summary>
		/// Message type.
		/// </summary>
		public Type MessageType
		{
			get { return _messageType; }
			set
			{
				_messageType = value;

				_fields.AddRange(value
					.GetMembers<PropertyInfo>(BindingFlags.Public | BindingFlags.Instance)
					.Where(pi =>
					{
						if (_ignoringFields.Contains(pi.Name))
							return false;

						var ba = pi.GetAttribute<BrowsableAttribute>();
						return ba == null || ba.Browsable;
					})
					.Select(pi =>
					{
						var nameAttr = pi.GetAttribute<DisplayNameAttribute>();
						return Tuple.Create(nameAttr == null ? pi.Name : nameAttr.DisplayName, pi);
					}));
			}
		}

		/// <summary>
		/// Message property, which will be made a comparison with the value of <see cref="AlertSettingsPanel.Value"/> based on the criterion <see cref="AlertSettingsPanel.Operator"/>.
		/// </summary>
		public PropertyInfo Property
		{
			get { return (PropertyInfo)GetValue(PropertyProperty); }
			set { SetValue(PropertyProperty, value); }
		}

		/// <summary>
		/// The criterion of comparison values <see cref="AlertSettingsPanel.Value"/>.
		/// </summary>
		public ComparisonOperator? Operator
		{
			get { return (ComparisonOperator?)GetValue(OperatorProperty); }
			set { SetValue(OperatorProperty, value); }
		}

		/// <summary>
		/// Comparison value.
		/// </summary>
		public object Value
		{
			get { return GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}

		private void PropertyCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var selectedValue = (Tuple<string, PropertyInfo>)PropertyCtrl.SelectedItem;

			if (selectedValue == null)
				return;

			var field = selectedValue.Equals(_nullField) ? null : selectedValue.Item2;

			Property = field;

			if (field == null)
			{
				OperatorCtrl.IsEnabled = false;
				return;
			}

			_operators.Clear();
			OperatorCtrl.IsEnabled = true;

			var type = field.PropertyType;

			if (type.IsNullable())
				type = type.GetUnderlyingType();

			EnumValue.Visibility = TimeValue.Visibility = DateValue.Visibility = DecimalValue.Visibility =
			TextValue.Visibility = /*SecurityValue.Visibility = PortfolioValue.Visibility = */Visibility.Collapsed;

			if (type == typeof(string) || type.IsEnum)// || typeof(Security).IsAssignableFrom(type) || typeof(Portfolio).IsAssignableFrom(type))
			{
				if (type == typeof(string))
					TextValue.Visibility = Visibility.Visible;
				//else if (typeof(Security).IsAssignableFrom(type))
				//	SecurityValue.Visibility = Visibility.Visible;
				//else if (typeof(Portfolio).IsAssignableFrom(type))
				//	PortfolioValue.Visibility = Visibility.Visible;
				else if (type.IsEnum)
				{
					EnumValue.Visibility = Visibility.Visible;
					EnumValue.EnumType = type;
				}

				_operators.Add(ComparisonOperator.Equal);
				_operators.Add(ComparisonOperator.NotEqual);
				_operators.Add(ComparisonOperator.Any);
			}
			else if (
				type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
				type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
				type == typeof(decimal) || type == typeof(double) || type == typeof(float))
			{
				_operators.AddRange(Enumerator.GetValues<ComparisonOperator>());
				DecimalValue.Visibility = Visibility.Visible;
			}
			else if (type == typeof(TimeSpan))
			{
				_operators.AddRange(Enumerator.GetValues<ComparisonOperator>());
				TimeValue.Visibility = Visibility.Visible;
			}
			else if (type == typeof(DateTime))
			{
				_operators.AddRange(Enumerator.GetValues<ComparisonOperator>());
				DateValue.Visibility = Visibility.Visible;
			}
		}

		private void OperatorCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Operator = (ComparisonOperator?)OperatorCtrl.SelectedItem;

			switch (Operator)
			{
				case ComparisonOperator.Equal:
				case ComparisonOperator.NotEqual:
				case ComparisonOperator.Greater:
				case ComparisonOperator.GreaterOrEqual:
				case ComparisonOperator.Less:
				case ComparisonOperator.LessOrEqual:
					ValuePanel.Visibility = Visibility.Visible;
					break;
				case ComparisonOperator.Any:
				case null:
					ValuePanel.Visibility = Visibility.Collapsed;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void TextValue_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			Value = TextValue.Text;
		}

		private void DecimalValue_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Value = DecimalValue.Value;
		}

		//private void PortfolioValue_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		//{
		//	Value = PortfolioValue.SelectedPortfolio;
		//}

		//private void SecurityValue_OnSecuritySelected()
		//{
		//	Value = SecurityValue.SelectedSecurity;
		//}

		private void TimeValue_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Value = TimeValue.Value;
		}

		private void DateValue_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Value = DateValue.Value;
		}

		private void EnumValue_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Value = EnumValue.SelectedItem;
		}
	}
}