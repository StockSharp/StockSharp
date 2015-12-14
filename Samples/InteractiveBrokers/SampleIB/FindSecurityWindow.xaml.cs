#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleIB.SampleIBPublic
File: FindSecurityWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleIB
{
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Messages;

	public partial class FindSecurityWindow
	{
		public FindSecurityWindow()
		{
			InitializeComponent();
			SecType.SetDataSource<SecurityTypes>();

			SecCode.Text = "AAPL";
			//SecType.SelectedValue = SecurityTypes.Stock;
			BoardName.Text = "SMART";
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			var secId = new SecurityId();
			//{
			//	SecurityCode = SecCode.Text,
			//};

			if (!BoardName.Text.IsEmpty())
				secId.BoardCode = BoardName.Text;

			if (!ContractId.Text.IsEmpty())
				secId.Native = ContractId.Text.To<int>();

			MainWindow.Instance.Trader.LookupSecurities(new SecurityLookupMessage
			{
				SecurityId = secId,
				Name = SecCode.Text,
				Currency = CurrencyTypes.USD,
				SecurityType = SecType.GetSelectedValue<SecurityTypes>() ?? default(SecurityTypes),
				TransactionId = MainWindow.Instance.Trader.TransactionIdGenerator.GetNextId(),
			});
			DialogResult = true;
		}

		private void BoardName_TextChanged(object sender, TextChangedEventArgs e)
		{
			TryEnableOk();
		}

		private void SecCode_TextChanged(object sender, TextChangedEventArgs e)
		{
			TryEnableOk();
		}

		private void SecType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnableOk();
		}

		private void ContractId_TextChanged(object sender, TextChangedEventArgs e)
		{
			TryEnableOk();
		}

		private void TryEnableOk()
		{
			Ok.IsEnabled = !SecCode.Text.IsEmpty() || !ContractId.Text.IsEmpty();
		}
	}
}