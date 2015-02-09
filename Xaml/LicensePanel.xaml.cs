namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Controls;
	using System.Windows.Media;

	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Licensing;

	using StockSharp.Localization;

	/// <summary>
	/// Графический компонент для представления информации о <see cref="License"/>.
	/// </summary>
	public partial class LicensePanel
	{
		private readonly Brush _estimatedNormalBrush;

		/// <summary>
		/// Создать <see cref="LicensePanel"/>.
		/// </summary>
		public LicensePanel()
		{
			InitializeComponent();
			_estimatedNormalBrush = Estimated.Foreground;
		}

		/// <summary>
		/// Лицензии.
		/// </summary>
		public IEnumerable<License> Licenses
		{
			get { return (IEnumerable<License>)LicensesCtrl.ItemsSource; }
			set
			{
				LicensesCtrl.ItemsSource = value;

				if (LicensesCtrl.ItemsSource.Cast<License>().Any())
					LicensesCtrl.SelectedIndex = 0;
			}
		}

		private void LicensesCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ApplySelectedLicense();
		}

		private void ApplySelectedLicense()
		{
			var license = (License)LicensesCtrl.SelectedItem;

			LicenseId.Text = license == null ? string.Empty : license.Id.To<string>();
			IssuedDate.Text = license == null ? string.Empty : license.IssuedDate.ToString("dd.MM.yyyy");
			IssuedTo.Text = license == null ? string.Empty : license.IssuedTo;
			ExpirationDate.Text = license == null ? string.Empty : license.ExpirationDate.ToString("dd.MM.yyyy");

			if (license == null)
			{
				Estimated.Text = string.Empty;
				Features.Items.Clear();
			}
			else
			{
				var estimatedDays = (int)license.GetEstimatedTime().TotalDays;
				Estimated.Text = estimatedDays == 0 ? LocalizedStrings.Str1536 : (estimatedDays + LocalizedStrings.Str1537);
				Estimated.Foreground = license.CanRenew() ? Brushes.Red : _estimatedNormalBrush;

				license.Features.ForEach(f => Features.Items.Add(f));
			}
		}
	}
}