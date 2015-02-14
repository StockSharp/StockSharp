namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using NCalc;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// Индекс, построенный из комбинации нескольких инструментов через математическую формулу <see cref="Expression"/>.
	/// </summary>
	#region Ignore
	//[Ignore(FieldName = "Code")]
	[Ignore(FieldName = "Class")]
	[Ignore(FieldName = "Name")]
	[Ignore(FieldName = "ShortName")]
	//[Ignore(FieldName = "Board")]
	[Ignore(FieldName = "ExtensionInfo")]
	[Ignore(FieldName = "Decimals")]
	[Ignore(FieldName = "VolumeStep")]
	[Ignore(FieldName = "PriceStep")]
	[Ignore(FieldName = "StepPrice")]
	[Ignore(FieldName = "OpenPrice")]
	[Ignore(FieldName = "ClosePrice")]
	[Ignore(FieldName = "HighPrice")]
	[Ignore(FieldName = "LowPrice")]
	[Ignore(FieldName = "MaxPrice")]
	[Ignore(FieldName = "MinPrice")]
	[Ignore(FieldName = "MarginBuy")]
	[Ignore(FieldName = "MarginSell")]
	[Ignore(FieldName = "Type")]
	[Ignore(FieldName = "OptionType")]
	[Ignore(FieldName = "TheorPrice")]
	[Ignore(FieldName = "Volatility")]
	[Ignore(FieldName = "Strike")]
	[Ignore(FieldName = "UnderlyingSecurityId")]
	[Ignore(FieldName = "OpenInterest")]
	[Ignore(FieldName = "SettlementDate")]
	[Ignore(FieldName = "ExpiryDate")]
	[Ignore(FieldName = "State")]
	[Ignore(FieldName = "LastTrade")]
	[Ignore(FieldName = "BestBid")]
	[Ignore(FieldName = "BestAsk")]
	[Ignore(FieldName = "Currency")]
	[Ignore(FieldName = "Sedol")]
	[Ignore(FieldName = "Cusip")]
	[Ignore(FieldName = "Isin")]
	[Ignore(FieldName = "Ric")]
	[Ignore(FieldName = "Bloomberg")]
	[Ignore(FieldName = "IQFeed")]
	[Ignore(FieldName = "LastChangeTime")]
	[Ignore(FieldName = "InteractiveBrokers")]
	[Ignore(FieldName = "Plaza")]
	#endregion
	[DisplayNameLoc(LocalizedStrings.IndexKey)]
	[DescriptionLoc(LocalizedStrings.Str728Key)]
	public class ExpressionIndexSecurity : IndexSecurity
	{
		private readonly SynchronizedList<Security> _innerSecurities = new SynchronizedList<Security>(); 

		/// <summary>
		/// Создать <see cref="ExpressionIndexSecurity"/>.
		/// </summary>
		public ExpressionIndexSecurity()
		{
			Board = ExchangeBoard.Associated;
		}

		private string _expressionText;
		private Expression _expression;

		/// <summary>
		/// Математическая формула индекса.
		/// </summary>
		[Browsable(false)]
		public string Expression
		{
			get { return _expressionText; }
			set
			{
				_expressionText = value;
				_expression = new Expression(ExpressionHelper.Encode(value));

				_innerSecurities.Clear();
				_expression.Parameters.Clear();

				if (!_expression.HasErrors())
				{
					var registry = ConfigManager.GetService<IEntityRegistry>();

					foreach (var id in _expression.GetSecurityIds())
					{
						_expression.Parameters[id] = null;

						var security = registry.Securities.ReadById(id);

						if (security != null)
							_innerSecurities.Add(security);
					}
				}
			}
		}

		/// <summary>
		/// Инструменты, из которых создана данная корзина.
		/// </summary>
		public override IEnumerable<Security> InnerSecurities
		{
			get { return _innerSecurities.SyncGet(c => c.ToArray()); }
		}

		/// <summary>
		/// Вычислить значение корзины.
		/// </summary>
		/// <param name="prices">Цены составных инструментов корзины <see cref="BasketSecurity.InnerSecurities"/>.</param>
		/// <returns>Значение корзины.</returns>
		public override decimal? Calculate(IDictionary<Security, decimal> prices)
		{
			if (prices == null)
				throw new ArgumentNullException("prices");

			if (prices.Count != _expression.Parameters.Count || !_innerSecurities.All(prices.ContainsKey))
				return null;

			foreach (var pair in prices)
			{
				_expression.Parameters[pair.Key.Id] = (double)pair.Value;
			}

			var value = (double)_expression.Evaluate();
			return (value.IsInfinity() || value.IsNaN()) ? 0 : value.To<decimal>();
		}

		/// <summary>
		/// Создать копию объекта <see cref="Security"/>.
		/// </summary>
		/// <returns>Копия объекта.</returns>
		public override Security Clone()
		{
			var clone = new ExpressionIndexSecurity { Expression = Expression };
			CopyTo(clone);
			return clone;
		}
	}
}