#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.SmartCom.Xaml.SmartCom
File: SmartComAddressComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.SmartCom.Xaml
{
	using Ecng.Xaml;

	using StockSharp.Localization;

	/// <summary>
	/// Выпадающий список для выбора адреса сервера SmartCOM.
	/// </summary>
	public class SmartComAddressComboBox : EndPointComboBox
	{
		/// <summary>
		/// Создать <see cref="SmartComAddressComboBox"/>.
		/// </summary>
		public SmartComAddressComboBox()
		{
			AddAddress(SmartComAddresses.Matrix, "MatriX™");
			AddAddress(SmartComAddresses.Demo, LocalizedStrings.Demo);
			AddAddress(SmartComAddresses.Reserve1, LocalizedStrings.Backup);
			AddAddress(SmartComAddresses.Reserve2, LocalizedStrings.Stalker);

			SelectedAddress = SmartComAddresses.Matrix;
		}
	}
}