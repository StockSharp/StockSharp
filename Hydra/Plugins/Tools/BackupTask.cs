namespace StockSharp.Hydra.Tools
{
	using System;

	using Ecng.Xaml;

	using StockSharp.Localization;
	using StockSharp.Hydra.Core;

	[DisplayNameLoc(LocalizedStrings.Str3131Key)]
	class BackupTask : BaseHydraTask
    {
		public override TaskTypes Type
		{
			get { return TaskTypes.Tool; }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str3767; }
		}

		public override Uri Icon
		{
			get { return "backup_logo.png".GetResourceUrl(GetType()); }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			
		}
    }
}