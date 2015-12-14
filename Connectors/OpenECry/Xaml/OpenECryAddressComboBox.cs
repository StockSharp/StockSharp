#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.OpenECry.Xaml.OpenECry
File: OpenECryAddressComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.OpenECry.Xaml
{
	using Ecng.Xaml;
	using StockSharp.Localization;

	/// <summary>
	/// The drop-down list to select the OpenECry server address.
	/// </summary>
	public class OpenECryAddressComboBox : EndPointComboBox
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OpenECryAddressComboBox"/>.
		/// </summary>
		public OpenECryAddressComboBox()
		{
			AddAddress(OpenECryAddresses.Api, LocalizedStrings.Main);
			AddAddress(OpenECryAddresses.Sim, LocalizedStrings.Demo);

			SelectedAddress = OpenECryAddresses.Api;
		}
	}
}