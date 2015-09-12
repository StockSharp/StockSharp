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