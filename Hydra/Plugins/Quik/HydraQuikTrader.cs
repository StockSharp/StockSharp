namespace StockSharp.Hydra.Quik
{
	using System.Collections.Generic;

	using StockSharp.Quik;

	class HydraQuikTrader : QuikTrader
	{
		protected override void OnStartExport()
		{
			if (IsDde)
				StartExport(Tables);
			else
				base.OnStartExport();
		}

		protected override void OnStopExport()
		{
			if (IsDde)
				StopExport(Tables);
			else
				base.OnStopExport();
		}

		protected override bool IsConnectionAlive()
		{
			return false;
		}

		public bool IsDownloadSecurityChangesHistory { get; set; }

		private IEnumerable<DdeTable> Tables
		{
			get
			{
				return IsDownloadSecurityChangesHistory ? new[] { SecuritiesTable, SecuritiesChangeTable, TradesTable } : new[] { SecuritiesTable, TradesTable };
			}
		}
	}
}