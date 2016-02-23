#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.HydraPublic
File: App.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra
{
	using System.Collections.Generic;

	using Ecng.Localization;

	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class App
	{
		public App()
		{
			AppIcon = "/stocksharp_data.ico";
			CheckTargetPlatform = true;
		}

		private readonly TargetPlatformFeature[] _extendedFeatures =
		{
			new TargetPlatformFeature("Alor", Languages.Russian),
			new TargetPlatformFeature("Finam", Languages.Russian),
			new TargetPlatformFeature("MFD", Languages.Russian),
			new TargetPlatformFeature("RTS", Languages.Russian),
			new TargetPlatformFeature(LocalizedStrings.Str2825Key, Languages.Russian),
			new TargetPlatformFeature("UX", Languages.Russian),
			new TargetPlatformFeature("DukasCopy"),
			new TargetPlatformFeature("GainCapital"),
			new TargetPlatformFeature("MBTrading"),
			new TargetPlatformFeature("TrueFX"),
			new TargetPlatformFeature("Yahoo"),
			new TargetPlatformFeature("Google"),
			new TargetPlatformFeature("FinViz"),
			new TargetPlatformFeature("Quandl"),
			new TargetPlatformFeature("S#.Data server")
		};

		protected override IEnumerable<TargetPlatformFeature> ExtendedFeatures => _extendedFeatures;
	}
}