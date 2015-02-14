namespace StockSharp.Xaml.PropertyGrid
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Linq;

	using ActiproSoftware.Windows;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using Unit = StockSharp.Messages.Unit;

	using Xceed.Wpf.Toolkit.PropertyGrid;
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// Редактор для <see cref="CandleSeries"/>.
	/// </summary>
	partial class CandleSettingsEditor : ITypeEditor
	{
		private static readonly IDictionary<Type, string> _candleTypes = new Dictionary<Type, string>();

		static CandleSettingsEditor()
		{
			foreach (var type in new[]
			{
				typeof(TimeFrameCandle),
				typeof(TickCandle),
				typeof(VolumeCandle),
				typeof(RangeCandle),
				typeof(RenkoCandle),
				typeof(PnFCandle)
			})
			{
				_candleTypes.Add(type, type.GetDisplayName());
			}
		}

		private readonly Dictionary<Type, FrameworkElement> _visibility = new Dictionary<Type, FrameworkElement>();
		private bool _initializing;
		private bool _isLoaded;

		/// <summary>
		/// Создать <see cref="CandleSettingsEditor"/>.
		/// </summary>
		public CandleSettingsEditor()
		{
			InitializeComponent();
			CandleType.ItemsSource = _candleTypes;

			_visibility.Add(typeof(TimeFrameCandle), TimeFramePanel);
			_visibility.Add(typeof(TickCandle), IntValuePanel);
			_visibility.Add(typeof(VolumeCandle), DecimalValuePanel);
			_visibility.Add(typeof(RangeCandle), UnitValuePanel);
			_visibility.Add(typeof(RenkoCandle), UnitValuePanel);
			_visibility.Add(typeof(PnFCandle), PnfValuePanel);

			Settings = new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = TimeSpan.FromMinutes(1),
			};

			UnitValue.Value = new Unit(1);
			PnfBoxSize.Value = new Unit(1);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="Settings"/>.
		/// </summary>
		public static readonly DependencyProperty SettingsProperty =
			DependencyProperty.Register("Settings", typeof(CandleSeries),
			typeof(CandleSettingsEditor), new UIPropertyMetadata(new CandleSeries { CandleType = typeof(TimeFrameCandle), Arg = TimeSpan.FromMinutes(1) }, OnSettingsChanged));

		private static void OnSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var editor = (CandleSettingsEditor)d;
			var settings = (CandleSeries)e.NewValue;

			editor._initializing = true;

			try
			{
				if (settings == null)
				{
					editor.Content = string.Empty;
					editor.IsEnabled = false;
					return;
				}

				editor.CandleType.SelectedItem = _candleTypes.First(p => p.Key == settings.CandleType);

				if (settings.CandleType == typeof(TimeFrameCandle))
				{
					editor.TimeFrame.Value = (TimeSpan)settings.Arg;
				}
				else if (settings.CandleType == typeof(TickCandle))
				{
					editor.IntValue.Value = settings.Arg.To<int>();
				}
				else if (settings.CandleType == typeof(VolumeCandle))
				{
					editor.DecimalValue.Value = settings.Arg.To<decimal>();
				}
				else if (settings.CandleType == typeof(RangeCandle))
				{
					editor.UnitValue.Value = settings.Arg.To<Unit>();
				}
				else if (settings.CandleType == typeof(PnFCandle))
				{
					var value = settings.Arg.To<PnFArg>();
					editor.PnfReversalAmount.Value = value.ReversalAmount;
					editor.PnfBoxSize.Value = value.BoxSize;
				}
				else if (settings.CandleType == typeof(RenkoCandle))
				{
					editor.UnitValue.Value = settings.Arg.To<Unit>();
				}

				if (editor._isLoaded)
					editor.UpdateVisibility();

				editor.Content = "{0} {1}".Put(_candleTypes[settings.CandleType], settings.Arg);
			}
			finally
			{
				editor._initializing = false;
			}
		}

		/// <summary>
		/// Настройки. По-умолчанию равно настройкам 1 минутных свечек.
		/// </summary>
		public CandleSeries Settings
		{
			get { return (CandleSeries)GetValue(SettingsProperty); }
			set { SetValue(SettingsProperty, value); }
		}

		FrameworkElement ITypeEditor.ResolveEditor(PropertyItem propertyItem)
		{
			BindingOperations.SetBinding(this, SettingsProperty, new Binding("Value")
			{
				Source = propertyItem,
				ValidatesOnExceptions = true,
				ValidatesOnDataErrors = true,
				Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay,
			});

			return this;
		}

		private void CandleType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_initializing)
				return;

			Settings.CandleType = ((KeyValuePair<Type, string>)CandleType.SelectedItem).Key;
			FillSettingsArg();

			UpdateVisibility();
		}

		private void UpdateVisibility()
		{
			var activeCtrl = _visibility[Settings.CandleType];
			activeCtrl.Visibility = Visibility.Visible;

			foreach (var elem in _visibility.Values)
			{
				if (!Equals(activeCtrl, elem))
					elem.Visibility = Visibility.Collapsed;
			}
		}

		private void UnitValueChanged(Unit value)
		{
			FillSettingsArg();
		}

		private void ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			FillSettingsArg();
		}

		private void TimeFrameValueChanged(object sender, PropertyChangedRoutedEventArgs<TimeSpan?> e)
		{
			FillSettingsArg();
		}

		private void FillSettingsArg()
		{
			if (_initializing)
				return;

			if (Settings.CandleType == typeof(TimeFrameCandle))
			{
				Settings.Arg = TimeFrame.Value ?? TimeSpan.Zero;
			}
			else if (Settings.CandleType == typeof(VolumeCandle))
			{
				Settings.Arg = DecimalValue.Value ?? 0m;
			}
			else if (Settings.CandleType == typeof(TickCandle))
			{
				Settings.Arg = IntValue.Value ?? 0;
			}
			else if (Settings.CandleType == typeof(RangeCandle) || Settings.CandleType == typeof(RenkoCandle))
			{
				Settings.Arg = UnitValue.Value ?? new Unit();
			}
			else if (Settings.CandleType == typeof(PnFCandle))
			{
				var arg = Settings.Arg as PnFArg ?? new PnFArg();

				arg.ReversalAmount = PnfReversalAmount.Value ?? 0;
				arg.BoxSize = PnfBoxSize.Value ?? new Unit();
				
				Settings.Arg = arg;
			}

			Content = "{0} {1}".Put(_candleTypes[Settings.CandleType], Settings.Arg);
		}

		// NOTE Какой-то баг в актипро. Если изменять видимость у TimeSpanEditBox до Loaded, то контрол будет нерердактируемый
		private void CandleSettingsEditor_OnLoaded(object sender, RoutedEventArgs e)
		{
			_isLoaded = true;

			if (Settings != null)
				_visibility[Settings.CandleType].Visibility = Visibility.Visible;
		}
	}
}