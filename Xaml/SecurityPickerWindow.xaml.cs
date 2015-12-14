#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: SecurityPickerWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Windows.Controls;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The instrument selection window.
	/// </summary>
	public partial class SecurityPickerWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityPicker"/>.
		/// </summary>
		public SecurityPickerWindow()
		{
			InitializeComponent();
			ShowOk = true;
		}

		/// <summary>
		/// The list items selection mode. The default is <see cref="DataGridSelectionMode.Extended"/>.
		/// </summary>
		public DataGridSelectionMode SelectionMode
		{
			get { return Picker.SelectionMode; }
			set { Picker.SelectionMode = value; }
		}

		/// <summary>
		/// The selected instrument.
		/// </summary>
		public Security SelectedSecurity
		{
			get { return Picker.SelectedSecurity; }
			set { Picker.SelectedSecurity = value; }
		}

		/// <summary>
		/// Selected instruments.
		/// </summary>
		public IList<Security> SelectedSecurities => Picker.SelectedSecurities;

		/// <summary>
		/// Instruments that should be hidden.
		/// </summary>
		public ISet<Security> ExcludeSecurities => Picker.ExcludeSecurities;

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider
		{
			get { return Picker.SecurityProvider; }
			set { Picker.SecurityProvider = value; }
		}

		/// <summary>
		/// The market data provider.
		/// </summary>
		public IMarketDataProvider MarketDataProvider
		{
			get { return Picker.MarketDataProvider; }
			set { Picker.MarketDataProvider = value; }
		}

		/// <summary>
		/// To show the OK button. By default, the button is shown.
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