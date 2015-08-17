namespace StockSharp.Algo.Storages.Backup.Xaml
{
	using System.Windows.Controls;

	/// <summary>
	/// Выпадающий список для выбора региона AWS.
	/// </summary>
	public class AmazonRegionComboBox : ComboBox
	{
		/// <summary>
		/// Создать <see cref="AmazonRegionComboBox"/>.
		/// </summary>
		public AmazonRegionComboBox()
		{
			DisplayMemberPath = "DisplayName";
			ItemsSource = AmazonExtensions.Endpoints;
		}
	}
}