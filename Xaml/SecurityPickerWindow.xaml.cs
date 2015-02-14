namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Windows.Controls;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Окно выбора инструмента.
	/// </summary>
	public partial class SecurityPickerWindow
	{
		/// <summary>
		/// Создать <see cref="SecurityPicker"/>.
		/// </summary>
		public SecurityPickerWindow()
		{
			InitializeComponent();
			ShowOk = true;
		}

		/// <summary>
		/// Режим выделения элементов списка. По-умолчанию равен <see cref="DataGridSelectionMode.Extended"/>.
		/// </summary>
		public DataGridSelectionMode SelectionMode
		{
			get { return Picker.SelectionMode; }
			set { Picker.SelectionMode = value; }
		}

		///<summary>
		/// Выбранный инструмент.
		///</summary>
		public Security SelectedSecurity
		{
			get { return Picker.SelectedSecurity; }
			set { Picker.SelectedSecurity = value; }
		}

		///<summary>
		/// Выбранные инструменты.
		///</summary>
		public IList<Security> SelectedSecurities
		{
			get { return Picker.SelectedSecurities; }
		}

		/// <summary>
		/// Доступные инструменты.
		/// </summary>
		public ISecurityList Securities
		{
			get { return Picker.Securities; }
		}

		/// <summary>
		/// Поставщик информации об инструментах.
		/// </summary>
		public FilterableSecurityProvider SecurityProvider
		{
			get { return Picker.SecurityProvider; }
			set { Picker.SecurityProvider = value; }
		}

		/// <summary>
		/// Показывать ли кнопку OK. По-умолчанию кнопка показывается.
		/// </summary>
		public bool ShowOk
		{
			get { return OkBtn.GetVisibility(); }
			set { OkBtn.SetVisibility(value); }
		}

		private void PickerSecurityDoubleClick(Security security)
		{
			if (!ShowOk)
				return;

			SelectedSecurity = security;
			DialogResult = true;
		}

		private void PickerSecuritySelected(Security security)
		{
			OkBtn.IsEnabled = security != null;
		}
	}
}