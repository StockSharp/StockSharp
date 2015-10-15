namespace StockSharp.Algo.Storages.Backup.Xaml
{
	using System.Windows.Controls;

	/// <summary>
	/// The drop-down list to select the AWS region.
	/// </summary>
	public class AmazonRegionComboBox : ComboBox
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AmazonRegionComboBox"/>.
		/// </summary>
		public AmazonRegionComboBox()
		{
			DisplayMemberPath = "DisplayName";
			ItemsSource = AmazonExtensions.Endpoints;
		}
	}
}