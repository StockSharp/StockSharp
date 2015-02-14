namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;

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

	class CustomExportMessage : BaseConnectionMessage
	{
		public CustomExportType ExportType { get; private set; }

		public DdeCustomTable Table { get; private set; }

		public IEnumerable<DdeTable> Tables { get; private set; }

		public string Caption { get; private set; }

		private CustomExportMessage(bool startExport, CustomExportType exportType)
			: base(startExport ? MessageTypes.Connect : MessageTypes.Disconnect)
		{
			ExportType = exportType;
		}

		public CustomExportMessage(bool startExport, DdeCustomTable table)
			: this(startExport, CustomExportType.Table)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			Table = table;
		}

		public CustomExportMessage(bool startExport, IEnumerable<DdeTable> tables)
			: this(startExport, CustomExportType.Tables)
		{
			if (tables == null)
				throw new ArgumentNullException("tables");

			Tables = tables;
		}

		public CustomExportMessage(bool startExport, string caption)
			: this(startExport, CustomExportType.Caption)
		{
			if (caption == null)
				throw new ArgumentNullException("caption");

			Caption = caption;
		}
	}
}