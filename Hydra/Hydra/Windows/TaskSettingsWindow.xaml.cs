namespace StockSharp.Hydra.Windows
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.ComponentModel;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class TaskSettingsWindow
	{
		private HydraTaskSettings _clonnedSettings;
		private bool _isError;

		private readonly Dictionary<Type, string> _dataTypes = new Dictionary<Type, string>
		{
			{ typeof(Trade), LocalizedStrings.Str985 },
			{ typeof(MarketDepth), LocalizedStrings.MarketDepths },
			{ typeof(OrderLogItem), LocalizedStrings.OrderLog },
			{ typeof(Level1ChangeMessage), LocalizedStrings.Level1 },
			{ typeof(Candle), LocalizedStrings.Candles },
			{ typeof(ExecutionMessage), LocalizedStrings.Transactions },
			{ typeof(NewsMessage), LocalizedStrings.News },
		};

		public TaskSettingsWindow()
		{
			InitializeComponent();
		}

		private IHydraTask _task;

		public IHydraTask Task
		{
			get { return _task; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_task = value;
				_clonnedSettings = _task.Settings.Clone();

				TaskSettings.IsEnabled = true;
				TaskSettings.SelectedObject = _clonnedSettings;

				DescriptionCtrl.Text = _task.GetDescription();
				AbilitiesCtrl.Text = _task.SupportedMarketDataTypes.Select(t => _dataTypes[t]).Join(", ");

				Help.DocUrl = _task.GetType().GetDocUrl();
			}
		}

		private void OkClick(object sender, RoutedEventArgs e)
		{
			OK.Focus();

			if (_isError)
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str2938)
					.Error()
					.Owner(this)
					.Show();

				return;
			}

			Task.Settings.ApplyChanges(_clonnedSettings);
			DialogResult = true;
		}

		private void TaskSettings_PropertyValueChanged(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyValueChangedEventArgs e)
		{
			if (TaskSettings.SelectedPropertyItem == null)
				return;

			_isError = false;

			// Х.з. как по уму обновить PropertyGrid.
			var pdc = TypeDescriptor.GetProperties(_clonnedSettings);

			if (!pdc
					 .Cast<PropertyDescriptor>()
					 .Any(propertyDescriptor => propertyDescriptor
													.Attributes
													.OfType<DisplayNameAttribute>()
													.Select(a => a.DisplayName)
													.Contains(TaskSettings.SelectedPropertyItem.DisplayName) &&
												propertyDescriptor
													.Attributes
													.OfType<AuxiliaryAttribute>()
													.Count() != 0)) return;

			TaskSettings.SelectedObject = new object();
			TaskSettings.SelectedObject = _clonnedSettings;
		}

		private void SourceSettings_OnError(object sender, ValidationErrorEventArgs e)
		{
			_isError = true;
		}
	}
}