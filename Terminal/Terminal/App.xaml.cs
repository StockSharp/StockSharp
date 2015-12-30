#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Terminal.TerminalPublic
File: App.xaml.cs
Created: 2015, 11, 11, 3:22 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using Ecng.Configuration;
using StockSharp.BusinessEntities;
using StockSharp.Studio.Core.Commands;
using StockSharp.Terminal.Fakes;
using System.Windows;

namespace StockSharp.Terminal
{
	public partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			ConfigManager.RegisterService<IStudioCommandService>(new FakeStudioCommandService());
			ConfigManager.RegisterService<ISecurityProvider>(new FakeSecurityProvider());
			ConfigManager.RegisterService<IMarketDataProvider>(new FakeMarketDataProvider());

			base.OnStartup(e);
		}
	}
}