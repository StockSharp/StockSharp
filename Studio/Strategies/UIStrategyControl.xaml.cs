#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Strategies.StrategiesPublic
File: UIStrategyControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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
				throw new ArgumentNullException(nameof(strategy));

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