#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleOptionQuoting.SampleOptionQuotingPublic
File: QuotesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleOptionQuoting
{
	using System.ComponentModel;

	using StockSharp.Algo.Derivatives;
	using StockSharp.BusinessEntities;

	public partial class QuotesWindow
	{
		private MarketDepth _depth;

		public QuotesWindow()
		{
			InitializeComponent();
		}

		private static IConnector Connector => MainWindow.Instance.Connector;

		public void Init(Security security)
		{
			_depth = Connector.GetMarketDepth(security);
			_depth.QuotesChanged += OnQuotesChanged;
			Connector.RegisterMarketDepth(_depth.Security);
		}

		private void OnQuotesChanged()
		{
			DepthCtrl.UpdateDepth(_depth.ImpliedVolatility(Connector, Connector, Connector.CurrentTime));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_depth.QuotesChanged -= OnQuotesChanged;
			Connector.UnRegisterMarketDepth(_depth.Security);
			base.OnClosing(e);
		}
	}
}