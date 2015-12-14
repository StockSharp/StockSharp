#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Actipro.Xaml.ActiproPublic
File: ActiproBaseApplication.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml.Actipro
{
	using System.Windows;

	using StockSharp.Xaml.Charting;

	/// <summary>
	/// The class for Actipro based applications.
	/// </summary>
	public abstract class ActiproBaseApplication : ExtendedBaseApplication
	{
		/// <summary>
		/// Processing the application start.
		/// </summary>
		/// <param name="e">Argument.</param>
		protected override void OnStartup(StartupEventArgs e)
		{
			Extensions.TranslateActiproDocking();
			Extensions.TranslateActiproNavigation();

			base.OnStartup(e);
		}
	}
}