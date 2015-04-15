namespace StockSharp.Hydra.Quik
{
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.Quik;

	class HydraQuikTrader : QuikTrader
	{
		protected override void OnConnect()
		{
			DdeTables = Tables;
		}

		public bool IsDownloadSecurityChangesHistory { get; set; }

		private IEnumerable<DdeTable> Tables
		{
			get
			{
				IEnumerable<DdeTable> tables = new[] { SecuritiesTable, TradesTable, OrdersTable, StopOrdersTable, MyTradesTable };

				if (IsDownloadSecurityChangesHistory)
					tables = tables.Concat(new[] { SecuritiesChangeTable });

				return tables;
			}
		}
	}
}