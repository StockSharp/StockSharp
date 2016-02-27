#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.PropertyGrid.Xaml
File: PropertyGridEx.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml.PropertyGrid
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	using Xceed.Wpf.Toolkit;
	using Xceed.Wpf.Toolkit.Primitives;
	using Xceed.Wpf.Toolkit.PropertyGrid;

	using Selector = System.Windows.Controls.Primitives.Selector;

	class EnumComboBoxEx : ComboBox
	{
		public static readonly DependencyProperty SelectedEnumItemProperty =
			DependencyProperty.Register(nameof(SelectedEnumItem), typeof(object), typeof(EnumComboBoxEx),
			new PropertyMetadata((s, e) =>
			{
				var ctrl = s as EnumComboBoxEx;
				if (ctrl == null)
					return;

				if (e.NewValue != null && ctrl.ItemsSource == null)
					ctrl.SetDataSource(e.NewValue.GetType());

				ctrl.SelectedValue = e.NewValue;
			}));

		public object SelectedEnumItem
		{
			get { return GetValue(SelectedEnumItemProperty); }
			set { SetValue(SelectedEnumItemProperty, value); }
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);
			SelectedEnumItem = this.GetSelectedValue();
		}
	}

	/// <summary>
	/// The extended table of settings.
	/// </summary>
	public partial class PropertyGridEx
	{
		//private sealed class DefaultValueConverter : IValueConverter
		//{
		//	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		//	{
		//		var item = value as PropertyItem;

		//		if (item == null)
		//			return null;

		//		var attr = item.PropertyDescriptor.Attributes.OfType<DefaultValueAttribute>().FirstOrDefault();

		//		return attr != null ? attr.Value : null;
		//	}

		//	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		//	{
		//		throw new NotSupportedException();
		//	}
		//}

		private static readonly ReadOnlyTypeDescriptionProvider _provider = new ReadOnlyTypeDescriptionProvider();

		/// <summary>
		/// To open the enclosed properties by default.
		/// </summary>
		public bool AutoExpandProperties { get; set; }

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SecurityProvider"/>.
		/// </summary>
		public static readonly DependencyProperty SecurityProviderProperty = DependencyProperty.Register(nameof(SecurityProvider), typeof(ISecurityProvider), typeof(PropertyGridEx));

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider
		{
			get { return (ISecurityProvider)GetValue(SecurityProviderProperty); }
			set { SetValue(SecurityProviderProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="ExchangeInfoProvider"/>.
		/// </summary>
		public static readonly DependencyProperty ExchangeInfoProviderProperty = DependencyProperty.Register(nameof(ExchangeInfoProvider), typeof(IExchangeInfoProvider), typeof(PropertyGridEx));

		/// <summary>
		/// The exchange boards provider.
		/// </summary>
		public IExchangeInfoProvider ExchangeInfoProvider
		{
			get { return (IExchangeInfoProvider)GetValue(ExchangeInfoProviderProperty); }
			set { SetValue(ExchangeInfoProviderProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Portfolios"/>.
		/// </summary>
		public static readonly DependencyProperty PortfoliosProperty = DependencyProperty.Register(nameof(Portfolios), typeof(ThreadSafeObservableCollection<Portfolio>), typeof(PropertyGridEx));

		/// <summary>
		/// Available portfolios.
		/// </summary>
		public ThreadSafeObservableCollection<Portfolio> Portfolios
		{
			get { return (ThreadSafeObservableCollection<Portfolio>)GetValue(PortfoliosProperty); }
			set { SetValue(PortfoliosProperty, value); }
		}

		/// <summary>
		/// The value change handle.
		/// </summary>
		/// <param name="oldValue">Previous value.</param>
		/// <param name="newValue">The new value.</param>
		protected override void OnSelectedObjectChanged(object oldValue, object newValue)
		{
			base.OnSelectedObjectChanged(oldValue, newValue);

			oldValue.DoIf<object, INotifyPropertiesChanged>(o => o.PropertiesChanged -= OnPropertiesChanged);
			newValue.DoIf<object, INotifyPropertiesChanged>(o => o.PropertiesChanged += OnPropertiesChanged);

			if (!AutoExpandProperties)
				return;
			
			Properties
				.OfType<PropertyItemBase>()
				.Where(i => i.IsExpandable)
				.ForEach(i => i.IsExpanded = true);
		}

		private void OnPropertiesChanged()
		{
			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				var value = SelectedObject;

				SelectedObject = null;
				SelectedObject = value;
			});
		}

		private static void SetDescriptionsProvider(bool isReadOnly)
		{
			TypeDescriptor.RemoveProvider(_provider, typeof(object));

			if (isReadOnly)
				TypeDescriptor.AddProvider(_provider, typeof(object));
		}

		private static EditorTemplateDefinition CreateNullableDateTimeEditor()
		{
			var binding = new Binding
			{
				Path = new PropertyPath("Value"),
				Mode = BindingMode.TwoWay
			};

			var element = new FrameworkElementFactory(typeof(DateTimePicker));
			element.SetBinding(DateTimePicker.ValueProperty, binding);
			element.SetValue(BorderThicknessProperty, new Thickness(0));
			element.SetValue(InputBase.TextAlignmentProperty, TextAlignment.Right);
			//element.SetValue(PartEditBox.CheckBoxVisibilityProperty, Visibility.Visible);
			element.SetValue(MarginProperty, new Thickness(5, 0, 0, 0));

			var dataTemplate = new DataTemplate { VisualTree = element };
			dataTemplate.Seal();

			return new EditorTemplateDefinition
			{
				EditingTemplate = dataTemplate,
				TargetProperties = { new TargetPropertyType { Type = typeof(DateTime?), } },
			};
		}

		private static EditorTemplateDefinition CreateNullableTimeSpanEditor()
		{
			var binding = new Binding
			{
				Path = new PropertyPath("Value"),
				Mode = BindingMode.TwoWay
			};

			var element = new FrameworkElementFactory(typeof(TimeSpanUpDown));
			element.SetBinding(TimeSpanUpDown.ValueProperty, binding);
			//element.SetBinding(TimeSpanEditBox.InitialValueProperty, new Binding(".") { Converter = new DefaultValueConverter() });
			element.SetValue(BorderThicknessProperty, new Thickness(0));
			//element.SetValue(TimeSpanUpDown.FormatProperty, "d hh:mm:ss");
			element.SetValue(InputBase.TextAlignmentProperty, TextAlignment.Right);
			//element.SetValue(PartEditBox.CheckBoxVisibilityProperty, Visibility.Visible);
			element.SetValue(MarginProperty, new Thickness(5, 0, 0, 0));

			var dataTemplate = new DataTemplate { VisualTree = element };
			dataTemplate.Seal();

			return new EditorTemplateDefinition
			{
				EditingTemplate = dataTemplate,
				TargetProperties = { new TargetPropertyType { Type = typeof(TimeSpan?), } },
			};
		}

		private static EditorTemplateDefinition CreateNullableDateTimeOffsetEditor()
		{
			var binding = new Binding
			{
				Path = new PropertyPath("Value"),
				Mode = BindingMode.TwoWay
			};

			var element = new FrameworkElementFactory(typeof(DateTimeOffsetEditor));
			element.SetBinding(DateTimeOffsetEditor.OffsetProperty, binding);
			element.SetValue(BorderThicknessProperty, new Thickness(0));
			element.SetValue(InputBase.TextAlignmentProperty, TextAlignment.Right);
			//element.SetValue(PartEditBox.CheckBoxVisibilityProperty, Visibility.Visible);
			element.SetValue(MarginProperty, new Thickness(5, 0, 0, 0));

			var dataTemplate = new DataTemplate { VisualTree = element };
			dataTemplate.Seal();

			return new EditorTemplateDefinition
			{
				EditingTemplate = dataTemplate,
				TargetProperties = { new TargetPropertyType { Type = typeof(DateTimeOffset?), } },
			};
		}

		private static EditorTemplateDefinition CreateExtensionInfoEditor()
		{
			var binding = new Binding
			{
				Path = new PropertyPath("Value"),
				Mode = BindingMode.TwoWay
			};

			var element = new FrameworkElementFactory(typeof(ExtensionInfoPicker));
			element.SetBinding(ExtensionInfoPicker.SelectedExtensionInfoProperty, binding);

			var dataTemplate = new DataTemplate { VisualTree = element };
			dataTemplate.Seal();

			return new EditorTemplateDefinition
			{
				TargetProperties = { "ExtensionInfo" },
				EditingTemplate = dataTemplate,
			};
		}

		private static EditorTemplateDefinition CreateNullableEnumEditor<T>(bool isEditable = false)
		{
			var binding = new Binding
			{
				Path = new PropertyPath("Value"),
				Mode = BindingMode.TwoWay
			};

			var element = new FrameworkElementFactory(typeof(EnumComboBox));
			element.SetValue(EnumComboBox.EnumTypeProperty, typeof(T).GetUnderlyingType());
			element.SetValue(ComboBox.IsEditableProperty, isEditable);
			element.SetBinding(Selector.SelectedValueProperty, binding);
			element.SetValue(BorderThicknessProperty, new Thickness(0));
			element.SetValue(MarginProperty, new Thickness(0));

			var dataTemplate = new DataTemplate { VisualTree = element };
			dataTemplate.Seal();

			return new EditorTemplateDefinition
			{
				EditingTemplate = dataTemplate,
				TargetProperties = { new TargetPropertyType { Type = typeof(T) } },
			};
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyGridEx"/>.
		/// </summary>
		public PropertyGridEx()
		{
			InitializeComponent();

			if (this.IsDesignMode())
				return;

			SetDescriptionsProvider(false);

			EditorDefinitions.Add(CreateNullableDateTimeEditor());
			EditorDefinitions.Add(CreateNullableTimeSpanEditor());
			EditorDefinitions.Add(CreateNullableDateTimeOffsetEditor());
			EditorDefinitions.Add(CreateExtensionInfoEditor());
			EditorDefinitions.Add(CreateNullableEnumEditor<SecurityTypes?>());
			EditorDefinitions.Add(CreateNullableEnumEditor<OptionTypes?>());
			EditorDefinitions.Add(CreateNullableEnumEditor<TPlusLimits?>());
			EditorDefinitions.Add(CreateNullableEnumEditor<CurrencyTypes?>(true));

			SecurityProvider = ConfigManager.TryGetService<ISecurityProvider>();
			ExchangeInfoProvider = ConfigManager.TryGetService<IExchangeInfoProvider>();
			Portfolios = ConfigManager.TryGetService<ThreadSafeObservableCollection<Portfolio>>();

			ConfigManager.ServiceRegistered += (t, s) =>
			{
				var sp = s as ISecurityProvider;

				if (sp != null)
				{
					this.GuiAsync(() =>
					{
						if (SecurityProvider == null)
							SecurityProvider = sp;
					});
				}

				var ep = s as IExchangeInfoProvider;

				if (ep != null)
				{
					this.GuiAsync(() =>
					{
						if (ExchangeInfoProvider == null)
							ExchangeInfoProvider = ep;
					});
				}

				var portfolios = s as ThreadSafeObservableCollection<Portfolio>;

				if (portfolios != null)
				{
					this.GuiAsync(() =>
					{
						if (Portfolios == null)
							Portfolios = portfolios;
					});
				}
			};
		}
	}
}