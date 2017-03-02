#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleBlackwood.SampleBlackwoodPublic
File: Level1Window.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleBlackwood
{
	public partial class Level1Window
	{
		public Level1Window()
		{
			InitializeComponent();

			Level1Grid.ColumnVisibility[0] = true;
			Level1Grid.ColumnVisibility[1] = true;

			Level1Grid.ColumnVisibility[3] = false;
			Level1Grid.ColumnVisibility[4] = false;

			for (int i = 11; i < Level1Grid.ColumnVisibility.Count; i++)
				Level1Grid.ColumnVisibility[i] = false;
		}
	}
}