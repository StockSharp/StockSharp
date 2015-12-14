#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: DdeSettings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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