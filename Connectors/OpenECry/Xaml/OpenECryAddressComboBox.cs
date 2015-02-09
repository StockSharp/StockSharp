namespace StockSharp.OpenECry.Xaml
{
	using Ecng.Xaml;
	using StockSharp.Localization;

	/// <summary>
	/// Выпадающий список для выбора адреса сервера OpenECry.
	/// </summary>
	public class OpenECryAddressComboBox : EndPointComboBox
	{
		/// <summary>
		/// Создать <see cref="OpenECryAddressComboBox"/>.
		/// </summary>
		public OpenECryAddressComboBox()
		{
			AddAddress(OpenECryAddresses.Api, LocalizedStrings.Main);
			AddAddress(OpenECryAddresses.Sim, LocalizedStrings.Demo);

			SelectedAddress = OpenECryAddresses.Api;
		}
	}
}