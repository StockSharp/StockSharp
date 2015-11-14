namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Messages;

	class HistoryLevel1ChangeMessage : Level1ChangeMessage
	{
	}

	enum CustomExportType
	{
		Table,
		Tables,
		Caption
	}

	class CustomExportMessage : MarketDataMessage
	{
		public CustomExportType ExportType { get; private set; }

		public DdeCustomTable Table { get; private set; }

		public IEnumerable<DdeTable> Tables { get; private set; }

		public string Caption { get; private set; }

		private CustomExportMessage(CustomExportType exportType)
		{
			ExportType = exportType;
		}

		public CustomExportMessage(DdeCustomTable table)
			: this(CustomExportType.Table)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));

			Table = table;
		}

		public CustomExportMessage(IEnumerable<DdeTable> tables)
			: this(CustomExportType.Tables)
		{
			if (tables == null)
				throw new ArgumentNullException(nameof(tables));

			Tables = tables;
		}

		public CustomExportMessage(string caption)
			: this(CustomExportType.Caption)
		{
			if (caption.IsEmpty())
				throw new ArgumentNullException(nameof(caption));

			Caption = caption;
		}
	}
}