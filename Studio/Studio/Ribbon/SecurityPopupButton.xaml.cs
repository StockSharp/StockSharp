#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: SecurityPopupButton.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using StockSharp.Localization;

namespace StockSharp.Studio.Ribbon
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Input;

	using ActiproSoftware.Windows;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	using RibbonButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.Button;
	using RibbonMenu = ActiproSoftware.Windows.Controls.Ribbon.Controls.Menu;
	using RibbonPopupButton = ActiproSoftware.Windows.Controls.Ribbon.Controls.PopupButton;

	public partial class SecurityPopupButton
	{
		public static readonly RoutedCommand OpenCommand = new RoutedCommand();

		private readonly Type _securityType;
		private readonly Func<IEnumerable<Security>> _getSecurities;
		private readonly Action<Security> _open;

		public SecurityPopupButton(Type securityType, Func<IEnumerable<Security>> getSecurities, Action<Security> open)
		{
			if (securityType == null)
				throw new ArgumentNullException(nameof(securityType));

			if (getSecurities == null)
				throw new ArgumentNullException(nameof(getSecurities));

			if (open == null)
				throw new ArgumentNullException(nameof(open));

			_securityType = securityType;
			_getSecurities = getSecurities;
			_open = open;

			InitializeComponent();
		}

		private void ExecutedOpenCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var security = (Security)e.Parameter;

			_open(security.Id == LocalizedStrings.Str3590 ? _securityType.CreateInstance<Security>() : security);
		}

		private void SecurityPopupButton_OnPopupOpening(object sender, CancelRoutedEventArgs e)
		{
			var btn = sender as RibbonPopupButton;

			if (btn == null)
				return;

			var menu = btn.PopupContent as RibbonMenu;

			if (menu == null)
				return;

			var items = new List<Security>
			{
				CreateStubSecurity()
			};

			var securities = _getSecurities().ToArray();

			if (securities.Length > 0)
			{
				items.Add(null); // separator
				items.AddRange(securities);
			}

			menu.ItemsSource = items;
		}

		private Security CreateStubSecurity()
		{
			var security = _securityType.CreateInstance<Security>();
			security.Id = LocalizedStrings.Str3590;
			return security;
		}
	}
}
