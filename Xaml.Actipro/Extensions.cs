#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Actipro.Xaml.ActiproPublic
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml.Actipro
{
	using System;
	using System.Globalization;

	using ActiproSoftware.Windows.Controls.Docking;
	using ActiproSoftware.Windows.Controls.Docking.Serialization;
	using ActiproSoftware.Windows.Controls.Navigation;
	using ActiproSoftware.Windows.Controls.Navigation.Serialization;

	using Ecng.Common;
	using Ecng.Localization;

	using StockSharp.Localization;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class Extensions
	{
		internal static void TranslateActiproDocking()
		{
			if (LocalizedStrings.ActiveLanguage == Languages.English)
				return;

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
			if (LocalizedStrings.ActiveLanguage == Languages.English)
				return;

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
		/// To save the Docking panel layout.
		/// </summary>
		/// <param name="dockSite">Docking panel.</param>
		/// <param name="toolWindowOnly">To save windows only.</param>
		/// <returns>Layout encoded as a string.</returns>
		public static string SaveLayout(this DockSite dockSite, bool toolWindowOnly = false)
		{
			if (dockSite == null)
				throw new ArgumentNullException(nameof(dockSite));

			return CultureInfo.InvariantCulture.DoInCulture(() => CreateDockSiteSerializer(toolWindowOnly).SaveToString(dockSite));
		}

		/// <summary>
		/// To download layout for the Docking panel.
		/// </summary>
		/// <param name="dockSite">Docking panel.</param>
		/// <param name="toolWindowOnly">To download windows only.</param>
		/// <param name="layout">Layout encoded as a string.</param>
		public static void LoadLayout(this DockSite dockSite, string layout, bool toolWindowOnly = false)
		{
			if (dockSite == null)
				throw new ArgumentNullException(nameof(dockSite));

			if (layout == null)
				throw new ArgumentNullException(nameof(layout));

			CultureInfo.InvariantCulture.DoInCulture(() => CreateDockSiteSerializer(toolWindowOnly).LoadFromString(layout, dockSite));
		}

		private static NavigationBarLayoutSerializer CreateNavigationSerializer()
		{
			return new NavigationBarLayoutSerializer();
		}

		/// <summary>
		/// To save the NavigationBar panel layout.
		/// </summary>
		/// <param name="navigationBar">NavigationBar panel.</param>
		/// <returns>Layout encoded as a string.</returns>
		public static string SaveLayout(this NavigationBar navigationBar)
		{
			if (navigationBar == null)
				throw new ArgumentNullException(nameof(navigationBar));

			return CultureInfo.InvariantCulture.DoInCulture(() => CreateNavigationSerializer().SaveToString(navigationBar));
		}

		/// <summary>
		/// To download the NavigationBar panel layout.
		/// </summary>
		/// <param name="navigationBar">NavigationBar panel.</param>
		/// <param name="layout">Layout encoded as a string.</param>
		public static void LoadLayout(this NavigationBar navigationBar, string layout)
		{
			if (navigationBar == null)
				throw new ArgumentNullException(nameof(navigationBar));

			if (layout == null)
				throw new ArgumentNullException(nameof(layout));

			CultureInfo.InvariantCulture.DoInCulture(() => CreateNavigationSerializer().LoadFromString(layout, navigationBar));
		}
	}
}