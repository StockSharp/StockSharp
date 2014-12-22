namespace StockSharp.Hydra
{
	using System.Collections.Generic;
	using StockSharp.Localization;

	public partial class App
	{
		public App()
		{
			AppIcon = "/stocksharp_data.ico";
			CheckTargetPlatform = true;
		}

		private readonly string[] _features = { "Finam", "Rts", LocalizedStrings.Str2825, "UX", "S#.Data" };

		protected override IEnumerable<string> ExtendedFeaturesX64
		{
			get
			{
				// в Гидре Квик работает и под 64 бита, так как используется только DDE.
				//return _features.Concat("Quik");

				// LUA работает под оба режима, поэтому Quik фича вынесена в базовый класс
				return _features;
			}
		}

		protected override IEnumerable<string> ExtendedFeaturesX86
		{
			get
			{
				return _features;
			}
		}
	}
}