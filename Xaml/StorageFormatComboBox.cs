#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: StorageFormatComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using Ecng.Xaml;

	using StockSharp.Algo.Storages;

	/// <summary>
	/// The drop-down list to select the instrument type.
	/// </summary>
	public class StorageFormatComboBox : EnumComboBox
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StorageFormatComboBox"/>.
		/// </summary>
		public StorageFormatComboBox()
		{
			EnumType = typeof(StorageFormats);
			SelectedFormat = StorageFormats.Binary;
		}

		/// <summary>
		/// The selected format.
		/// </summary>
		public StorageFormats SelectedFormat
		{
			get { return this.GetSelectedValue<StorageFormats>() ?? StorageFormats.Binary; }
			set { this.SetSelectedValue<StorageFormats>(value); }
		}
	}
}