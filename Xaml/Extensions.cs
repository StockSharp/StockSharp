namespace StockSharp.Xaml
{
	using System;
	using System.Globalization;

	using ActiproSoftware.Windows.Controls.Docking;
	using ActiproSoftware.Windows.Controls.Docking.Serialization;
	using ActiproSoftware.Windows.Controls.Navigation;
	using ActiproSoftware.Windows.Controls.Navigation.Serialization;

	using Ecng.Common;

	using wyDay.Controls;

	using StockSharp.Localization;

	/// <summary>
	/// Вспомогательный класс.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Локализовать <see cref="AutomaticUpdater"/>.
		/// </summary>
		/// <param name="automaticUpdater"><see cref="AutomaticUpdater"/>.</param>
		public static void Translate(this AutomaticUpdater automaticUpdater)
		{
			if (automaticUpdater == null)
				throw new ArgumentNullException("automaticUpdater");

			automaticUpdater.Translation.AlreadyUpToDate = LocalizedStrings.Str1466;
			automaticUpdater.Translation.CancelCheckingMenu = LocalizedStrings.Str1467;
			automaticUpdater.Translation.CancelUpdatingMenu = LocalizedStrings.Str1468;
			automaticUpdater.Translation.ChangesInVersion = LocalizedStrings.Str1469;
			automaticUpdater.Translation.CheckForUpdatesMenu = LocalizedStrings.Str1470;
			automaticUpdater.Translation.Checking = LocalizedStrings.Str1471;
			automaticUpdater.Translation.CloseButton = LocalizedStrings.Str1472;
			automaticUpdater.Translation.Downloading = LocalizedStrings.Str1473;
			automaticUpdater.Translation.DownloadUpdateMenu = LocalizedStrings.Str1474;
			automaticUpdater.Translation.ErrorTitle = LocalizedStrings.Str152;
			automaticUpdater.Translation.Extracting = LocalizedStrings.Str1475;
			automaticUpdater.Translation.FailedToCheck = LocalizedStrings.Str1476;
			automaticUpdater.Translation.FailedToDownload = LocalizedStrings.Str1477;
			automaticUpdater.Translation.FailedToExtract = LocalizedStrings.Str1478;
			automaticUpdater.Translation.HideMenu = LocalizedStrings.Str1479;
			automaticUpdater.Translation.InstallOnNextStart = LocalizedStrings.Str1480;
			automaticUpdater.Translation.InstallUpdateMenu = LocalizedStrings.Str1481;
			automaticUpdater.Translation.PrematureExitMessage = "";
			automaticUpdater.Translation.PrematureExitTitle = "";
			automaticUpdater.Translation.StopChecking = LocalizedStrings.Str1482;
			automaticUpdater.Translation.StopDownloading = LocalizedStrings.Str1483;
			automaticUpdater.Translation.StopExtracting = LocalizedStrings.Str1484;
			automaticUpdater.Translation.SuccessfullyUpdated = LocalizedStrings.Str1485;
			automaticUpdater.Translation.TryAgainLater = LocalizedStrings.Str1486;
			automaticUpdater.Translation.TryAgainNow = LocalizedStrings.Str1487;
			automaticUpdater.Translation.UpdateAvailable = LocalizedStrings.Str1488;
			automaticUpdater.Translation.UpdateFailed = LocalizedStrings.Str1489;
			automaticUpdater.Translation.UpdateNowButton = LocalizedStrings.Str1490;
			automaticUpdater.Translation.ViewChangesMenu = LocalizedStrings.Str1491;
			automaticUpdater.Translation.ViewError = LocalizedStrings.Str1492;
		}

		internal static void TranslateActiproDocking()
		{
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandActivateNextDocumentText.ToString(), LocalizedStrings.Str1493);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandActivatePreviousDocumentText.ToString(), LocalizedStrings.Str1494);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandActivatePrimaryDocumentText.ToString(), LocalizedStrings.Str1495);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandClosePrimaryDocumentText.ToString(), LocalizedStrings.Str1496);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandCloseWindowText.ToString(), LocalizedStrings.Str1472);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandMakeDockedWindowText.ToString(), LocalizedStrings.Str1497);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandMakeDocumentWindowText.ToString(), LocalizedStrings.Str1498);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandMakeFloatingWindowText.ToString(), LocalizedStrings.Str1499);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandMaximizeWindowText.ToString(), LocalizedStrings.Str1500);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandMinimizeWindowText.ToString(), LocalizedStrings.Str1501);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandMoveToNewHorizontalContainerText.ToString(), LocalizedStrings.Str1502);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandMoveToNewVerticalContainerText.ToString(), LocalizedStrings.Str1503);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandMoveToNextContainerText.ToString(), LocalizedStrings.Str1504);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandMoveToPreviousContainerText.ToString(), LocalizedStrings.Str1505);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandOpenDocumentsMenuText.ToString(), LocalizedStrings.Str1506);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandOpenOptionsMenuText.ToString(), LocalizedStrings.Str1507);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandOpenToolsMenuText.ToString(), LocalizedStrings.Str1508);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandOpenWindowText.ToString(), LocalizedStrings.Str1509);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandRestoreWindowText.ToString(), LocalizedStrings.Str1510);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UICommandToggleWindowAutoHideStateText.ToString(), LocalizedStrings.Str1511);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIStandardSwitcherDocumentsText.ToString(), LocalizedStrings.Str1506);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIStandardSwitcherToolWindowsText.ToString(), LocalizedStrings.Str1508);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UITabbedMdiContainerCloseButtonToolTip.ToString(), LocalizedStrings.Str1472);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UITabbedMdiContainerDocumentsButtonToolTip.ToString(), LocalizedStrings.Str1506);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIToolWindowContainerAutoHideButtonToolTip.ToString(), LocalizedStrings.Str1511);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIToolWindowContainerCloseButtonToolTip.ToString(), LocalizedStrings.Str1472);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIToolWindowContainerOptionsButtonToolTip.ToString(), LocalizedStrings.Str1507);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIToolWindowContainerToolsButtonToolTip.ToString(), LocalizedStrings.Str1508);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIWindowControlCloseButtonToolTip.ToString(), LocalizedStrings.Str1472);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIWindowControlMaximizeButtonToolTip.ToString(), LocalizedStrings.Str1500);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIWindowControlMinimizeButtonToolTip.ToString(), LocalizedStrings.Str1501);
			ActiproSoftware.Products.Docking.SR.SetCustomString(ActiproSoftware.Products.Docking.SRName.UIWindowControlRestoreButtonToolTip.ToString(), LocalizedStrings.Str1510);
		}

		internal static void TranslateActiproNavigation()
		{
			//Navigation.SR.SetCustomString(Navigation.SRName.ExZoomContentControlMouseButtonMustBePressed.ToString(), "");
			//Navigation.SR.SetCustomString(Navigation.SRName.ExZoomDecoratorFactorOutOfRange.ToString(), "");
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarCustomizeButtonToolTip.ToString(), LocalizedStrings.Str1512);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarCustomizeMenuItemAddRemoveButtonsText.ToString(), LocalizedStrings.Str1513);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarCustomizeMenuItemOptionsText.ToString(), LocalizedStrings.Settings);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarCustomizeMenuItemShowFewerButtonsText.ToString(), LocalizedStrings.Str1514);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarCustomizeMenuItemShowMoreButtonsText.ToString(), LocalizedStrings.Str1515);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarOptionsWindowCancelButtonText.ToString(), LocalizedStrings.Cancel);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarOptionsWindowDisplayButtonsLabelText.ToString(), LocalizedStrings.Str1516);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarOptionsWindowMoveDownButtonText.ToString(), LocalizedStrings.Str1517);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarOptionsWindowMoveUpButtonText.ToString(), LocalizedStrings.Str1518);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarOptionsWindowOkButtonText.ToString(), LocalizedStrings.Str1519);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarOptionsWindowResetButtonText.ToString(), LocalizedStrings.Str1520);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarOptionsWindowTitle.ToString(), LocalizedStrings.Settings);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarToggleMinimizationButtonExpandToolTip.ToString(), LocalizedStrings.Str1500);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarToggleMinimizationButtonMinimizeToolTip.ToString(), LocalizedStrings.Str1501);
			ActiproSoftware.Products.Navigation.SR.SetCustomString(ActiproSoftware.Products.Navigation.SRName.UINavigationBarToggleMinimizedPopupButtonExpandToolTip.ToString(), LocalizedStrings.Str1500);
			//Navigation.SR.SetCustomString(Navigation.SRName.UIZoomContentControlResetViewButtonToolTip.ToString(), "");
			//Navigation.SR.SetCustomString(Navigation.SRName.UIZoomContentControlToggleMinimizationButtonExpandToolTip.ToString(), "");
			//Navigation.SR.SetCustomString(Navigation.SRName.UIZoomContentControlToggleMinimizationButtonMinimizeToolTip.ToString(), "");
			//Navigation.SR.SetCustomString(Navigation.SRName.UIZoomContentControlZoomInButtonToolTip.ToString(), "");
			//Navigation.SR.SetCustomString(Navigation.SRName.UIZoomContentControlZoomOutButtonToolTip.ToString(), "");
			//Navigation.SR.SetCustomString(Navigation.SRName.UIZoomContentControlZoomToFitButtonToolTip.ToString(), "");
		}

		private static DockSiteLayoutSerializer CreateDockSiteSerializer(bool toolWindowOnly)
		{
			return new DockSiteLayoutSerializer
			{
				SerializationBehavior = toolWindowOnly ? DockSiteSerializationBehavior.ToolWindowsOnly : DockSiteSerializationBehavior.All,
				DocumentWindowDeserializationBehavior = DockingWindowDeserializationBehavior.LazyLoad,
				ToolWindowDeserializationBehavior = DockingWindowDeserializationBehavior.LazyLoad
			};
		}

		/// <summary>
		/// Сохранить разметку Docking панели.
		/// </summary>
		/// <param name="dockSite">Docking панель.</param>
		/// <param name="toolWindowOnly">Сохранять только окна.</param>
		/// <returns>Разметка ввиде строки.</returns>
		public static string SaveLayout(this DockSite dockSite, bool toolWindowOnly = false)
		{
			if (dockSite == null)
				throw new ArgumentNullException("dockSite");

			return CultureInfo.InvariantCulture.DoInCulture(() => CreateDockSiteSerializer(toolWindowOnly).SaveToString(dockSite));
		}

		/// <summary>
		/// Загрузить разметку для Docking панели.
		/// </summary>
		/// <param name="dockSite">Docking панель.</param>
		/// <param name="toolWindowOnly">Загружать только окна.</param>
		/// <param name="layout">Разметка ввиде строки.</param>
		public static void LoadLayout(this DockSite dockSite, string layout, bool toolWindowOnly = false)
		{
			if (dockSite == null)
				throw new ArgumentNullException("dockSite");

			if (layout == null)
				throw new ArgumentNullException("layout");

			CultureInfo.InvariantCulture.DoInCulture(() => CreateDockSiteSerializer(toolWindowOnly).LoadFromString(layout, dockSite));
		}

		private static NavigationBarLayoutSerializer CreateNavigationSerializer()
		{
			return new NavigationBarLayoutSerializer();
		}

		/// <summary>
		/// Сохранить разметку NavigationBar панели.
		/// </summary>
		/// <param name="navigationBar">NavigationBar панель.</param>
		/// <returns>Разметка ввиде строки.</returns>
		public static string SaveLayout(this NavigationBar navigationBar)
		{
			if (navigationBar == null)
				throw new ArgumentNullException("navigationBar");

			return CultureInfo.InvariantCulture.DoInCulture(() => CreateNavigationSerializer().SaveToString(navigationBar));
		}

		/// <summary>
		/// Загрузить разметку для NavigationBar панели.
		/// </summary>
		/// <param name="navigationBar">NavigationBar панель.</param>
		/// <param name="layout">Разметка ввиде строки.</param>
		public static void LoadLayout(this NavigationBar navigationBar, string layout)
		{
			if (navigationBar == null)
				throw new ArgumentNullException("navigationBar");

			if (layout == null)
				throw new ArgumentNullException("layout");

			CultureInfo.InvariantCulture.DoInCulture(() => CreateNavigationSerializer().LoadFromString(layout, navigationBar));
		}
	}
}