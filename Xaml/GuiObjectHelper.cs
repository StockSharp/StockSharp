namespace StockSharp.Xaml
{
	using System;
	using System.Windows;

	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	/// <summary>
	/// Вспомогательный класс для синхронизованных объектов.
	/// </summary>
	public static class GuiObjectHelper
	{
		/// <summary>
		/// Создать синхронизованное подключение <see cref="GuiConnector{TUnderlyingConnector}"/>.
		/// </summary>
		/// <typeparam name="TUnderlyingConnector">Тип подключения, который необходимо синхронизовать.</typeparam>
		/// <param name="connector">Подключение, которое необходимо обернуть в <see cref="GuiConnector{TUnderlyingConnector}"/>.</param>
		/// <returns>Cинхронизованное подключение <see cref="GuiConnector{TUnderlyingConnector}"/>.</returns>
		public static GuiConnector<TUnderlyingConnector> GuiSyncTrader<TUnderlyingConnector>(this TUnderlyingConnector connector)
			where TUnderlyingConnector : IConnector
		{
			return new GuiConnector<TUnderlyingConnector>(connector);
		}

		/// <summary>
		/// Показать модальный диалог в потоке подключения.
		/// </summary>
		/// <typeparam name="TWindow">Тип окна.</typeparam>
		/// <param name="createWindow">Обработчик, создающий окно.</param>
		/// <param name="wndClosed">Обработчик закрытия окна.</param>
		/// <returns>Результат закрытия окна.</returns>
		public static bool ShowDialog<TWindow>(Func<TWindow> createWindow, Action<TWindow> wndClosed)
			where TWindow : Window
		{
			if (createWindow == null)
				throw new ArgumentNullException("createWindow");

			if (wndClosed == null)
				throw new ArgumentNullException("wndClosed");

			var w1 = ConfigManager.TryGetService<Window>();

			var dispatcher = w1 != null && w1.Dispatcher != null ?
							w1.Dispatcher :
							Application.Current != null ? Application.Current.Dispatcher : null;

			if (dispatcher == null)
				throw new InvalidOperationException(LocalizedStrings.Str1564);

			var dialogOk = false;

			dispatcher.GuiSync(() =>
			{
				var w2 = Application.Current.MainWindow;
				var owner = (w1 != null && w1.IsVisible) ? w1 : (w2 != null && w2.IsVisible) ? w2 : null;

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