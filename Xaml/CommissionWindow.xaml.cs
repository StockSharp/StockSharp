namespace StockSharp.Xaml
{
	using Ecng.Collections;

	using StockSharp.Algo.Commissions;

	/// <summary>
	/// Окно для редактирования списка <see cref="ICommissionRule"/>.
	/// </summary>
	public partial class CommissionWindow
	{
		/// <summary>
		/// Создать <see cref="CommissionWindow"/>.
		/// </summary>
		public CommissionWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Список правил, добавленных в таблицу.
		/// </summary>
		public IListEx<ICommissionRule> Rules
		{
			get { return Panel.Rules; }
		}
	}
}