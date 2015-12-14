#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Localization;

	using wyDay.Controls;

	using StockSharp.Localization;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// To localize <see cref="AutomaticUpdater"/>.
		/// </summary>
		/// <param name="automaticUpdater"><see cref="AutomaticUpdater"/>.</param>
		public static void Translate(this AutomaticUpdater automaticUpdater)
		{
			if (automaticUpdater == null)
				throw new ArgumentNullException(nameof(automaticUpdater));

			if (LocalizedStrings.ActiveLanguage == Languages.English)
				return;

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

		/// <summary>
		/// Cast value to specified type.
		/// </summary>
		/// <typeparam name="T">Return type.</typeparam>
		/// <param name="value">Source value.</param>
		/// <returns>Casted value.</returns>
		public static T WpfCast<T>(this object value)
		{
			return value == DependencyProperty.UnsetValue ? default(T) : value.To<T>();
		}
	}
}