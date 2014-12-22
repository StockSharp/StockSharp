namespace SampleBlackwood
{
	using System.Linq;
	using System.Windows;

	using MoreLinq;

	public partial class Level1Window
	{
		public Level1Window()
		{
			InitializeComponent();

			Level1Grid.Columns[0].Visibility = Visibility.Visible;
			Level1Grid.Columns[1].Visibility = Visibility.Visible;

			Level1Grid.Columns[3].Visibility = Visibility.Collapsed;
			Level1Grid.Columns[4].Visibility = Visibility.Collapsed;

			Level1Grid.Columns.Skip(11).ForEach(c => c.Visibility = Visibility.Collapsed);
		}
	}
}