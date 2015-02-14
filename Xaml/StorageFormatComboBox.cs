namespace StockSharp.Xaml
{
	using Ecng.Xaml;

	using StockSharp.Algo.Storages;

	/// <summary>
	/// Выпадающий список для выбора типа инструмента.
	/// </summary>
	public class StorageFormatComboBox : EnumComboBox
	{
		/// <summary>
		/// Создать <see cref="StorageFormatComboBox"/>.
		/// </summary>
		public StorageFormatComboBox()
		{
			EnumType = typeof(StorageFormats);
			SelectedFormat = StorageFormats.Binary;
		}

		/// <summary>
		/// Выбранный формат.
		/// </summary>
		public StorageFormats SelectedFormat
		{
			get { return this.GetSelectedValue<StorageFormats>() ?? StorageFormats.Binary; }
			set { this.SetSelectedValue<StorageFormats>(value); }
		}
	}
}