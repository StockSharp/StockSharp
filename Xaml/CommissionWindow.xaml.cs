namespace StockSharp.Xaml
{
	using Ecng.Collections;

	using StockSharp.Algo.Commissions;

	/// <summary>
	/// The window for the list editing <see cref="ICommissionRule"/>.
	/// </summary>
	public partial class CommissionWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommissionWindow"/>.
		/// </summary>
		public CommissionWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// The list of rules added to the table.
		/// </summary>
		public IListEx<ICommissionRule> Rules => Panel.Rules;
	}
}