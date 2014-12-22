namespace OptionCalculator
{
	using System.Collections.ObjectModel;
	using System.ComponentModel;

	public partial class QuotesWindow
	{
		public QuotesWindow()
		{
			this.Quotes = new ObservableCollection<IVQuote>();
			InitializeComponent();
		}

		public ObservableCollection<IVQuote> Quotes { get; private set; }

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!this.RealClose)
			{
				base.Hide();
				e.Cancel = true;
			}

			base.OnClosing(e);
		}

		public bool RealClose { get; set; }
	}
}