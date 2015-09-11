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