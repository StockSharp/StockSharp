namespace StockSharp.Hydra.IQFeed
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	using CheckComboBox = Xceed.Wpf.Toolkit.CheckComboBox;

	/// <summary>
	/// Визуальный редактор для выбора набора типов инструментов.
	/// </summary>
	public class SecurityTypesComboBoxEditor : TypeEditor<SecurityTypesComboBox>
	{
		/// <summary>
		/// 
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = SecurityTypesComboBox.SelectedTypesProperty;
		}
	}

	/// <summary>
	/// Выпадающий список для выбора набора таблиц.
	/// </summary>
	public class SecurityTypesComboBox : CheckComboBox
	{
		static SecurityTypesComboBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SecurityTypesComboBox), new FrameworkPropertyMetadata(typeof(SecurityTypesComboBox)));
		}

		private readonly UniqueObservableCollection<SecurityTypes> _types = new UniqueObservableCollection<SecurityTypes>();

		/// <summary>
		/// Набор доступных таблиц.
		/// </summary>
		public ICollection<SecurityTypes> Types { get { return _types; } }

		/// <summary>
		/// Список идентификаторов выбранных таблиц.
		/// </summary>
		public static readonly DependencyProperty SelectedTypesProperty = DependencyProperty.Register("SelectedTypes", typeof(UniqueObservableCollection<SecurityTypes>),
																									  typeof(SecurityTypesComboBox), new UIPropertyMetadata(SelectedTablesChanged));

		private static void SelectedTablesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d as SecurityTypesComboBox;
			if (ctrl == null)
				return;

			var oldList = (UniqueObservableCollection<SecurityTypes>)e.OldValue;
			if (oldList != null)
				oldList.CollectionChanged -= ctrl.CollectionChanged;

			var list = (UniqueObservableCollection<SecurityTypes>)e.NewValue;
			list.CollectionChanged += ctrl.CollectionChanged;

			ctrl.SelectedItemsOverride = list;
			ctrl.UpdateText();
		}

		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateText();
		}

		/// <summary>
		/// Список выбранных таблиц.
		/// </summary>
		public UniqueObservableCollection<SecurityTypes> SelectedTypes
		{
			get { return (UniqueObservableCollection<SecurityTypes>)GetValue(SelectedTypesProperty); }
			set { SetValue(SelectedTypesProperty, value); }
		}

		/// <summary>
		/// Создать <see cref="SecurityTypesComboBox"/>.
		/// </summary>
		public SecurityTypesComboBox()
		{
			Types.AddRange(Enum.GetValues(typeof(SecurityTypes)).Cast<SecurityTypes>());
			ItemsSource = Types;

			SelectedTypes = new UniqueObservableCollection<SecurityTypes>
			{
				SecurityTypes.Stock
			};
		}

		/// <summary>
		/// Метод, который вызывается при изменении выбранного значения.
		/// </summary>
		/// <param name="oldValue">Старое значение.</param>
		/// <param name="newValue">Новое значение.</param>
		protected override void OnSelectedValueChanged(string oldValue, string newValue)
		{
			base.OnSelectedValueChanged(oldValue, newValue);
			UpdateText();
		}

		private void UpdateText()
		{
			Text = "Выбрано: " + (SelectedTypes != null ? SelectedTypes.Count : 0);
		}
	}
}
