#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: GuiObjectHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Windows;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The helper class for synchronized objects.
	/// </summary>
	public static class GuiObjectHelper
	{
		/// <summary>
		/// To create the synchronized connection <see cref="GuiConnector{T}"/>.
		/// </summary>
		/// <typeparam name="TUnderlyingConnector">Type of connection that should be synchronized.</typeparam>
		/// <param name="connector">The connection that should be wrapped in <see cref="GuiConnector{T}"/>.</param>
		/// <returns>The synchronized connection <see cref="GuiConnector{T}"/>.</returns>
		public static GuiConnector<TUnderlyingConnector> GuiSyncTrader<TUnderlyingConnector>(this TUnderlyingConnector connector)
			where TUnderlyingConnector : IConnector
		{
			return new GuiConnector<TUnderlyingConnector>(connector);
		}

		/// <summary>
		/// To show modal dialog in the connection flow.
		/// </summary>
		/// <typeparam name="TWindow">The window type.</typeparam>
		/// <param name="createWindow">The handler creating a window.</param>
		/// <param name="wndClosed">The handler of window closing.</param>
		/// <returns>The result of window closing.</returns>
		public static bool ShowDialog<TWindow>(Func<TWindow> createWindow, Action<TWindow> wndClosed)
			where TWindow : Window
		{
			if (createWindow == null)
				throw new ArgumentNullException(nameof(createWindow));

			if (wndClosed == null)
				throw new ArgumentNullException(nameof(wndClosed));

			var dispatcher = Application.Current != null ? Application.Current.Dispatcher : null;

			if (dispatcher == null)
				throw new InvalidOperationException(LocalizedStrings.Str1564);

			var dialogOk = false;

			dispatcher.GuiSync(() =>
			{
				var owner = Application.Current.MainWindow;

				var wnd = createWindow();

				if (owner != null)
					dialogOk = wnd.ShowModal(owner);
				else
					dialogOk = wnd.ShowDialog() == true;

				wndClosed(wnd);
			});

			return dialogOk;
		}
	}
}