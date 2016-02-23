#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: SecurityLookupPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The instrument search panel.
	/// </summary>
	public partial class SecurityLookupPanel : IPersistable
	{
		/// <summary>
		/// <see cref="RoutedCommand"/> for <see cref="SecurityLookupPanel.Lookup"/>.
		/// </summary>
		public static RoutedCommand SearchSecurityCommand = new RoutedCommand();

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityLookupPanel"/>.
		/// </summary>
		public SecurityLookupPanel()
		{
			InitializeComponent();

			Filter = new Security();
		}

		/// <summary>
		/// The filter for instrument search.
		/// </summary>
		private Security Filter
		{
			get { return (Security)SecurityFilterEditor.SelectedObject; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				SecurityFilterEditor.SelectedObject = value;
			}
		}

		/// <summary>
		/// The start of instrument search event.
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

			if (Filter.IsLookupAll())
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
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			SecurityCodeLike.Text = storage.GetValue<string>(nameof(SecurityCodeLike));
			Filter = storage.GetValue<Security>(nameof(Filter));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(SecurityCodeLike), SecurityCodeLike.Text);
			storage.SetValue(nameof(Filter), Filter.Clone());
		}
	}
}