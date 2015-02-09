namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit;
	using Xceed.Wpf.Toolkit.Primitives;

	using ComboItem = System.Collections.Generic.KeyValuePair<StockSharp.Messages.SecurityTypes, string>;

	using StockSharp.Localization;

	/// <summary>
	/// Выпадающий список для выбора типов инструмента.
	/// </summary>
	public class SecurityTypesComboBox : CheckComboBox
	{
		private static readonly IDictionary<SecurityTypes, string> _allTypes;

		static SecurityTypesComboBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SecurityTypesComboBox), new FrameworkPropertyMetadata(typeof(SecurityTypesComboBox)));

			_allTypes = Enumerator.GetValues<SecurityTypes>().ToDictionary(t => t, t => t.GetDisplayName());
		}

		/// <summary>
		/// Создать <see cref="SecurityTypesComboBox"/>.
		/// </summary>
		public SecurityTypesComboBox()
		{
			ValueMemberPath = "Key";
			DisplayMemberPath = "Value";

			ItemsSource = _allTypes;

			SelectedTypes = Enumerable.Empty<SecurityTypes>();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedTypes"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedTypesProperty = DependencyProperty.Register("SelectedTypes",
			typeof(IEnumerable<SecurityTypes>), typeof(SecurityTypesComboBox), new UIPropertyMetadata((s, e) =>
			{
				var comboBox = s as SecurityTypesComboBox;

				if (comboBox == null)
					return;

				var count = 0;

				var prevItems = comboBox.SelectedItems.Cast<ComboItem>().ToList();

				foreach (var type in (IEnumerable<SecurityTypes>)e.NewValue)
				{
					var item = new ComboItem(type, type.GetDisplayName());

					if (!comboBox.SelectedItems.Contains(item))
						comboBox.SelectedItems.Add(item);

					prevItems.Remove(item);

					count++;
				}

				prevItems.ForEach(item => comboBox.SelectedItems.Remove(item));

				comboBox.Text = LocalizedStrings.Str1544 + count;
			}));

		/// <summary>
		/// Список выбранных типов.
		/// </summary>
		public IEnumerable<SecurityTypes> SelectedTypes
		{
			get { return (IEnumerable<SecurityTypes>)GetValue(SelectedTypesProperty); }
			set { SetValue(SelectedTypesProperty, value.ToArray()); }
		}

		/// <summary>
		/// Метод, который вызывается при изменении значения у элемента.
		/// </summary>
		/// <param name="e">Аргументы.</param>
		protected override void OnItemSelectionChanged(ItemSelectionChangedEventArgs e)
		{
			base.OnItemSelectionChanged(e);
			SelectedTypes = SelectedItems.Cast<ComboItem>().Select(p => p.Key);
		}
	}
}