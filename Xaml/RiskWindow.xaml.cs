namespace StockSharp.Xaml
{
	using Ecng.Collections;

	using StockSharp.Algo.Risk;

	/// <summary>
	/// The window for the list editing <see cref="IRiskRule"/>.
	/// </summary>
	public partial class RiskWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RiskWindow"/>.
		/// </summary>
		public RiskWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// The list of rules added to the table.
		/// </summary>
		public IListEx<IRiskRule> Rules
		{
			get { return Panel.Rules; }
		}
	}
}
