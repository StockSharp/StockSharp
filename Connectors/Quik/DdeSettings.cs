namespace StockSharp.Quik
{
	class DdeSettings
	{
		public DdeSettings()
		{
			RowsCaption = ColumnsCaption = EmptyCells = false;
			FormalValues = true;
		}

		public string TableName { get; set; }

		public bool RowsCaption { get; set; }

		public bool ColumnsCaption { get; set; }

		public bool FormalValues { get; set; }

		public bool EmptyCells { get; set; }
	}
}