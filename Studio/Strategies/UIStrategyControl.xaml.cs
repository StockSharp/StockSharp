namespace StockSharp.Studio.Strategies
{
	using System;
	using System.Windows;

	using Ecng.Serialization;

	using StockSharp.Algo.Strategies;
	using StockSharp.Studio.Core;
	using StockSharp.Localization;

	public partial class UIStrategyControl : IStudioControl
	{
		private readonly UIStrategy _strategy;

		public UIStrategyControl()
		{
			InitializeComponent();
		}

		public UIStrategyControl(UIStrategy strategy)
			: this()
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			_strategy = strategy;
		}

		private void BuyAtMarket_Click(object sender, RoutedEventArgs e)
		{
			_strategy.RegisterOrder(_strategy.BuyAtMarket());
		}

		private void SellAtMarket_Click(object sender, RoutedEventArgs e)
		{
			_strategy.RegisterOrder(_strategy.SellAtMarket());
		}

		void IPersistable.Load(SettingsStorage storage)
		{
		}

		void IPersistable.Save(SettingsStorage storage)
		{
		}

		void IDisposable.Dispose()
		{
		}

		string IStudioControl.Title
		{
			get { return LocalizedStrings.Str3284; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}
	}
}