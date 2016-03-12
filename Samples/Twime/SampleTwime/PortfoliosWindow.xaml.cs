#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleTwime.SampleTwimePublic
File: PortfoliosWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleTwime
{
	using System.Windows;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	public partial class PortfoliosWindow
	{
		public PortfoliosWindow()
		{
			InitializeComponent();
		}

		private void Lookup_OnClick(object sender, RoutedEventArgs e)
		{
			if (LocalOnly.IsChecked == true)
				MainWindow.Instance.Trader.SendOutMessage(new PortfolioMessage { PortfolioName = NameLike.Text });
			else
				MainWindow.Instance.Trader.LookupPortfolios(new Portfolio { Name = NameLike.Text });
		}
	}
}