namespace StockSharp.Xaml
{
	using Ecng.Collections;

	using StockSharp.Algo.Risk;

	/// <summary>
	/// Окно для редактирования списка <see cref="IRiskRule"/>.
	/// </summary>
	public partial class RiskWindow
	{
		/// <summary>
		/// Создать <see cref="RiskWindow"/>.
		/// </summary>
		public RiskWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Список правил, добавленных в таблицу.
		/// </summary>
		public IListEx<IRiskRule> Rules
		{
			get { return Panel.Rules; }
		}
	}
}
