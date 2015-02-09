namespace StockSharp.Xaml
{
	using System.Linq;
	using System.Windows;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit;
	using Xceed.Wpf.Toolkit.Primitives;

	using ComboItem = System.Collections.Generic.KeyValuePair<StockSharp.Messages.Level1Fields, string>;

	using StockSharp.Localization;

	/// <summary>
	/// Выпадающий список для выбора набора полей <see cref="Level1Fields"/>.
	/// </summary>
	public class Level1FieldsComboBox : CheckComboBox
	{
		private static readonly IDictionary<Level1Fields, string> _allFields;

		static Level1FieldsComboBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Level1FieldsComboBox), new FrameworkPropertyMetadata(typeof(Level1FieldsComboBox)));

			_allFields = Enumerator.GetValues<Level1Fields>().ToDictionary(t => t, t => t.GetDisplayName());

			DefaultFields = _allFields.Keys.Except(new[]
			{
				Level1Fields.LastTrade,
				Level1Fields.LastTradeId,
				Level1Fields.LastTradeOrigin,
				Level1Fields.LastTradePrice,
				Level1Fields.LastTradeTime,
				Level1Fields.LastTradeUpDown,
				Level1Fields.LastTradeVolume,
				Level1Fields.BestBid,
				Level1Fields.BestBidPrice,
				Level1Fields.BestBidTime,
				Level1Fields.BestBidVolume,
				Level1Fields.BestAsk,
				Level1Fields.BestAskPrice,
				Level1Fields.BestAskTime,
				Level1Fields.BestAskVolume
			}).ToArray();
		}

		/// <summary>
		/// Создать <see cref="Level1FieldsComboBox"/>.
		/// </summary>
		public Level1FieldsComboBox()
		{
			ValueMemberPath = "Key";
			DisplayMemberPath = "Value";

			ItemsSource = _allFields;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedFields"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedFieldsProperty = DependencyProperty.Register("SelectedFields",
			typeof(IEnumerable<Level1Fields>), typeof(Level1FieldsComboBox), new UIPropertyMetadata(DefaultFields, (s, e) =>
			{
				var comboBox = s as Level1FieldsComboBox;

				if (comboBox == null)
					return;

				var count = 0;

				var prevItems = comboBox.SelectedItems.Cast<ComboItem>().ToList();

				foreach (var field in (IEnumerable<Level1Fields>)e.NewValue)
				{
					var item = new ComboItem(field, field.GetDisplayName());

					if (!comboBox.SelectedItems.Contains(item))
						comboBox.SelectedItems.Add(item);

					prevItems.Remove(item);

					count++;
				}

				prevItems.ForEach(item => comboBox.SelectedItems.Remove(item));

				comboBox.Text = LocalizedStrings.Str1544 + count;
			}));

		/// <summary>
		/// Список выбранных полей.
		/// </summary>
		public IEnumerable<Level1Fields> SelectedFields
		{
			get { return (IEnumerable<Level1Fields>)GetValue(SelectedFieldsProperty); }
			set { SetValue(SelectedFieldsProperty, value.ToArray()); }
		}

		/// <summary>
		/// Набор выбранных полей по-умолчанию.
		/// </summary>
		public static IEnumerable<Level1Fields> DefaultFields { get; private set; }

		/// <summary>
		/// Метод, который вызывается при изменении значения у элемента.
		/// </summary>
		/// <param name="e">Аргументы.</param>
		protected override void OnItemSelectionChanged(ItemSelectionChangedEventArgs e)
		{
			base.OnItemSelectionChanged(e);
			SelectedFields = SelectedItems.Cast<ComboItem>().Select(p => p.Key);
		}
	}
}