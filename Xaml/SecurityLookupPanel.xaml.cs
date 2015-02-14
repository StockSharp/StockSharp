namespace StockSharp.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Панель поиска инструментов.
	/// </summary>
	public partial class SecurityLookupPanel : IPersistable
	{
		/// <summary>
		/// <see cref="RoutedCommand"/> для <see cref="Lookup"/>.
		/// </summary>
		public static RoutedCommand SearchSecurityCommand = new RoutedCommand();

		/// <summary>
		/// Создать <see cref="SecurityLookupPanel"/>.
		/// </summary>
		public SecurityLookupPanel()
		{
			InitializeComponent();

			Filter = new Security();
		}

		/// <summary>
		/// Фильтр для поиска инструментов.
		/// </summary>
		private Security Filter
		{
			get { return (Security)SecurityFilterEditor.SelectedObject; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				SecurityFilterEditor.SelectedObject = value;
			}
		}

		/// <summary>
		/// Событие запуска поиска инструментов.
		/// </summary>
		public event Action<Security> Lookup;

		private void ExecutedSearchSecurity(object sender, ExecutedRoutedEventArgs e)
		{
			Lookup.SafeInvoke(Filter);
		}

		private void CanExecuteSearchSecurity(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = Filter != null;// && !SecurityCodeLike.Text.IsEmpty();
		}

		private void SecurityCodeLike_OnPreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter)
				return;

			Filter.Code = SecurityCodeLike.Text.Trim();

			if (Filter.Code == "*")
				Filter.Code = string.Empty;
			//else if (Filter.Code.IsEmpty())
			//	return;

			Lookup.SafeInvoke(Filter);
		}

		private void ClearFilter(object sender, RoutedEventArgs e)
		{
			Filter = new Security();
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			SecurityCodeLike.Text = storage.GetValue<string>("SecurityCodeLike");
			Filter = storage.GetValue<Security>("Filter");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("SecurityCodeLike", SecurityCodeLike.Text);
			storage.SetValue("Filter", Filter.Clone());
		}
	}
}