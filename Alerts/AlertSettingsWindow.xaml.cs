#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alerts.Alerts
File: AlertSettingsWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Alerts
{
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.Community;
	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// Schema modification window.
	/// </summary>
	public partial class AlertSettingsWindow
	{
		private readonly PairSet<AlertTypes, RadioButton> _buttons = new PairSet<AlertTypes, RadioButton>();
		private readonly IAlertService _alertService = ConfigManager.GetService<IAlertService>();
		private readonly ObservableCollection<AlertRule> _rules = new ObservableCollection<AlertRule>();

		private const string _testCaption = "RI. Open Interest > 100 000";

		/// <summary>
		/// Initializes a new instance of the <see cref="AlertSettingsWindow"/>.
		/// </summary>
		public AlertSettingsWindow()
		{
			InitializeComponent();

			_buttons.Add(AlertTypes.Sound, IsSound);
			_buttons.Add(AlertTypes.Speech, IsSpeech);
			_buttons.Add(AlertTypes.Popup, IsPopup);
			_buttons.Add(AlertTypes.Sms, IsSms);
			_buttons.Add(AlertTypes.Email, IsEmail);
			_buttons.Add(AlertTypes.Log, IsLog);

			RulesCtrl.ItemsSource = _rules;

			var client = new NotificationClient();

			if (AuthenticationClient.Instance.IsLoggedIn)
			{
				try
				{
					TestSms.Content = ((string)TestSms.Content).Put(client.SmsCount);
					TestEmail.Content = ((string)TestEmail.Content).Put(client.EmailCount);
					return;
				}
				catch (Exception ex)
				{
					ex.LogError();
				}

				TestSms.Content = TestEmail.Content = LocalizedStrings.Str152;
			}
			else
			{
				TestSms.Content = TestEmail.Content = LocalizedStrings.NotAuthorized;
			}

			TestSms.Foreground = TestEmail.Foreground = Brushes.Red;
			TestSms.FontWeight = TestEmail.FontWeight = FontWeights.Bold;
			IsSms.IsEnabled = IsEmail.IsEnabled = TestSms.IsEnabled = TestEmail.IsEnabled = false;
		}

		private AlertSchema _schema;

		/// <summary>
		/// Schema.
		/// </summary>
		public AlertSchema Schema
		{
			get { return _schema; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_schema = value;
				_rules.AddRange(value.Rules.Select(r => new AlertRule
				{
					Property = r.Property,
					Operator = r.Operator,
					Value = r.Value,
				}));

				if (_rules.IsEmpty())
					_rules.Add(new AlertRule());

				if (value.AlertType == null)
					IsOff.IsChecked = true;
				else
					_buttons[(AlertTypes)value.AlertType].IsChecked = true;

				Caption.Text = value.Caption;
				Message.Text = value.Message;

				TryEnableOk();
			}
		}

		private void TestPopup_OnClick(object sender, RoutedEventArgs e)
		{
			PushAlert(AlertTypes.Popup);
		}

		private void TestSound_OnClick(object sender, RoutedEventArgs e)
		{
			PushAlert(AlertTypes.Sound);
		}

		private void TestSms_OnClick(object sender, RoutedEventArgs e)
		{
			PushAlert(AlertTypes.Sms);
		}

		private void TestEmail_OnClick(object sender, RoutedEventArgs e)
		{
			PushAlert(AlertTypes.Email);
		}

		private void TestSpeech_OnClick(object sender, RoutedEventArgs e)
		{
			PushAlert(AlertTypes.Speech);
		}

		private void TestLog_OnClick(object sender, RoutedEventArgs e)
		{
			PushAlert(AlertTypes.Log);
		}

		private void PushAlert(AlertTypes type)
		{
			_alertService.PushAlert(type, _testCaption, LocalizedStrings.Str3035, TimeHelper.Now);
		}

		private void AddRule_OnClick(object sender, RoutedEventArgs e)
		{
			_rules.Add(new AlertRule());
			TryEnableOk();
		}

		private void RemoveRule_OnClick(object sender, RoutedEventArgs e)
		{
			_rules.Remove(SelectedRule);
			TryEnableOk();
		}

		private AlertRule SelectedRule => (AlertRule)RulesCtrl.SelectedValue;

		private void RulesCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			RemoveRule.IsEnabled = SelectedRule != null;
		}

		private void Ok_OnClick(object sender, RoutedEventArgs e)
		{
			foreach (var rule in _rules)
			{
				if (rule.Property == null)
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.SomeRuleNoSource)
						.Owner(this)
						.Error()
						.Show();

					return;
				}

				if (rule.Operator != ComparisonOperator.Any && rule.Value == null)
				{
					var nameAttr = rule.Property.GetAttribute<DisplayNameAttribute>();

					new MessageBoxBuilder()
						.Text(LocalizedStrings.ForRuleNotSetValue.Put(nameAttr == null ? rule.Property.Name : nameAttr.DisplayName))
						.Owner(this)
						.Error()
						.Show();

					return;
				}
			}

			_schema.Rules.Clear();
			_schema.Rules.AddRange(_rules);

			_schema.AlertType = IsOff.IsChecked == true ? (AlertTypes?)null : _buttons[_buttons.Values.First(b => b.IsChecked == true)];
			_schema.Caption = Caption.Text;
			_schema.Message = Message.Text;

			DialogResult = true;
		}

		private void Caption_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			TryEnableOk();
		}

		private void Message_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			TryEnableOk();
		}

		private void TryEnableOk()
		{
			Ok.IsEnabled = !Caption.Text.IsEmpty() && !Message.Text.IsEmpty() && !_rules.IsEmpty();
		}
	}
}