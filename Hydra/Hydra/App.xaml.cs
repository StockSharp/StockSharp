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
			new TargetPlatformFeature("S#.Data server")
		};

		protected override IEnumerable<TargetPlatformFeature> ExtendedFeatures
		{
			get { return _extendedFeatures; }
		}
	}
}