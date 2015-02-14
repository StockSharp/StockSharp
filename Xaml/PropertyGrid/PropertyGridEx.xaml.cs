namespace StockSharp.Xaml.PropertyGrid
{
	using System;
	using System.ComponentModel;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Data;

	using ActiproSoftware.Windows.Controls.Editors;
	using ActiproSoftware.Windows.Controls.Editors.Primitives;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Messages;
	using StockSharp.Xaml;

	using Xceed.Wpf.Toolkit.PropertyGrid;

	class EnumComboBoxEx : ComboBox
	{
		public static readonly DependencyProperty SelectedEnumItemProperty =
			DependencyProperty.Register("SelectedEnumItem", typeof(object), typeof(EnumComboBoxEx),
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
	/// Расширенная таблица настроек.
	/// </summary>
	public partial class PropertyGridEx
	{
		private sealed class DefaultValueConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				var item = value as PropertyItem;

				if (item == null)
					return null;

				var attr = item.PropertyDescriptor.Attributes.OfType<DefaultValueAttribute>().FirstOrDefault();

				return attr != null ? attr.Value : null;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotSupportedException();
			}
		}

		private static readonly ReadOnlyTypeDescriptionProvider _provider = new ReadOnlyTypeDescriptionProvider();

		/// <summary>
		/// Раскрывать вложенные свойства по-умолчанию.
		/// </summary>
		public bool AutoExpandProperties { get; set; }

		/// <summary>
		/// Обработчик изменения значения.
		/// </summary>
		/// <param name="oldValue">Предыдущее значение.</param>
		/// <param name="newValue">Новое значение.</param>
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

			var element = new FrameworkElementFactory(typeof(DateTimeEditBox));
			element.SetBinding(DateTimeEditBox.ValueProperty, binding);
			element.SetValue(BorderThicknessProperty, new Thickness(0));
			element.SetValue(SlottedItemsControl.CenterSlotHorizontalAlignmentProperty, HorizontalAlignment.Right);
			element.SetValue(PartEditBox.CheckBoxVisibilityProperty, Visibility.Visible);
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

			var element = new FrameworkElementFactory(typeof(TimeSpanEditBox));
			element.SetBinding(TimeSpanEditBox.ValueProperty, binding);
			element.SetBinding(TimeSpanEditBox.InitialValueProperty, new Binding(".") { Converter = new DefaultValueConverter() });
			element.SetValue(BorderThicknessProperty, new Thickness(0));
			element.SetValue(TimeSpanEditBox.FormatProperty, "d hh:mm:ss");
			element.SetValue(SlottedItemsControl.CenterSlotHorizontalAlignmentProperty, HorizontalAlignment.Right);
			element.SetValue(PartEditBox.CheckBoxVisibilityProperty, Visibility.Visible);
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
			element.SetValue(SlottedItemsControl.CenterSlotHorizontalAlignmentProperty, HorizontalAlignment.Right);
			element.SetValue(PartEditBox.CheckBoxVisibilityProperty, Visibility.Visible);
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
		/// Создать <see cref="PropertyGridEx"/>.
		/// </summary>
		public PropertyGridEx()
		{
			InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
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
		}
	}
}