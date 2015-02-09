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