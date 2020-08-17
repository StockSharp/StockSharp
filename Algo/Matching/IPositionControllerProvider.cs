namespace StockSharp.Algo.Matching
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;
	using StockSharp.Algo.Commissions;

	/// <summary>
	/// Interface described position controller provider.
	/// </summary>
	public interface IPositionControllerProvider
	{
		/// <summary>
		/// Commission manager.
		/// </summary>
		ICommissionManager CommissionManager { get; }

		/// <summary>
		/// All controllers.
		/// </summary>
		IEnumerable<IPositionController> Controllers { get; }

		/// <summary>
		/// Get controller for the specified portfolio name.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		/// <returns>Controller.</returns>
		IPositionController GetController(string portfolioName);

		/// <summary>
		/// Try get controller for the specified portfolio name.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		/// <param name="controller">Controller.</param>
		/// <returns><see langword="true"/> if controller was found, otherwise, <see langword="false"/>.</returns>
		bool TryGetController(string portfolioName, out IPositionController controller);

		/// <summary>
		/// Reset state.
		/// </summary>
		void Reset();
	}

	/// <summary>
	/// Default implementation of <see cref="IPositionControllerProvider"/>.
	/// </summary>
	public class PositionControllerProvider : IPositionControllerProvider
	{
		private readonly Func<SecurityId, SecurityMessage> _getSecurityDefinition;
		private readonly Func<SecurityId, Sides, decimal> _getMarginPrice;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="PositionControllerProvider"/>.
		/// </summary>
		/// <param name="getSecurityDefinition">Handler to get security info.</param>
		/// <param name="getMarginPrice">Handler to get margin info.</param>
		public PositionControllerProvider(Func<SecurityId, SecurityMessage> getSecurityDefinition, Func<SecurityId, Sides, decimal> getMarginPrice)
		{
			_getSecurityDefinition = getSecurityDefinition ?? throw new ArgumentNullException(nameof(getSecurityDefinition));
			_getMarginPrice = getMarginPrice ?? throw new ArgumentNullException(nameof(getMarginPrice));
		}

		/// <inheritdoc />
		public ICommissionManager CommissionManager { get; } = new CommissionManager();

		/// <summary>
		/// Check money balance.
		/// </summary>
		public bool CheckMoney { get; set; }

		/// <summary>
		/// Can have short positions.
		/// </summary>
		public bool CheckShortable { get; set; }

		private readonly Dictionary<string, IPositionController> _portfolios = new Dictionary<string, IPositionController>();

		/// <inheritdoc />
		public IEnumerable<IPositionController> Controllers => _portfolios.Values;

		/// <inheritdoc />
		public IPositionController GetController(string portfolioName)
		{
			if (!TryGetController(portfolioName, out var controller))
			{
				controller = new PositionController(portfolioName, CommissionManager, _getSecurityDefinition, _getMarginPrice)
				{
					CheckMoney = CheckMoney,
					CheckShortable = CheckShortable,
				};

				_portfolios.Add(portfolioName, controller);
			}
			
			return controller;
		}

		/// <inheritdoc />
		public bool TryGetController(string portfolioName, out IPositionController controller)
		{
			return _portfolios.TryGetValue(portfolioName, out controller);
		}

		/// <inheritdoc />
		public void Reset()
		{
			_portfolios.Clear();
		}
	}
}