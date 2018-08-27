namespace StockSharp.Algo.Expressions
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The index, built of combination of several instruments through mathematic formula <see cref="Expression"/>.
	/// </summary>
	#region Ignore
	//[Ignore(FieldName = nameof(Code))]
	[Ignore(FieldName = nameof(Class))]
	[Ignore(FieldName = nameof(Name))]
	[Ignore(FieldName = nameof(ShortName))]
	//[Ignore(FieldName = nameof(Board))]
	[Ignore(FieldName = nameof(ExtensionInfo))]
	[Ignore(FieldName = nameof(Decimals))]
	[Ignore(FieldName = nameof(VolumeStep))]
	[Ignore(FieldName = nameof(PriceStep))]
	[Ignore(FieldName = nameof(StepPrice))]
	[Ignore(FieldName = nameof(OpenPrice))]
	[Ignore(FieldName = nameof(ClosePrice))]
	[Ignore(FieldName = nameof(HighPrice))]
	[Ignore(FieldName = nameof(LowPrice))]
	[Ignore(FieldName = nameof(MaxPrice))]
	[Ignore(FieldName = nameof(MinPrice))]
	[Ignore(FieldName = nameof(MarginBuy))]
	[Ignore(FieldName = nameof(MarginSell))]
	[Ignore(FieldName = nameof(Type))]
	[Ignore(FieldName = nameof(OptionType))]
	[Ignore(FieldName = nameof(TheorPrice))]
	[Ignore(FieldName = nameof(ImpliedVolatility))]
	[Ignore(FieldName = nameof(HistoricalVolatility))]
	[Ignore(FieldName = nameof(Strike))]
	[Ignore(FieldName = nameof(UnderlyingSecurityId))]
	[Ignore(FieldName = nameof(OpenInterest))]
	[Ignore(FieldName = nameof(SettlementDate))]
	[Ignore(FieldName = nameof(ExpiryDate))]
	[Ignore(FieldName = nameof(State))]
	[Ignore(FieldName = nameof(LastTrade))]
	[Ignore(FieldName = nameof(BestBid))]
	[Ignore(FieldName = nameof(BestAsk))]
	[Ignore(FieldName = nameof(Currency))]
	[Ignore(FieldName = nameof(LastChangeTime))]
	[Ignore(FieldName = nameof(SecurityExternalId.Sedol))]
	[Ignore(FieldName = nameof(SecurityExternalId.Cusip))]
	[Ignore(FieldName = nameof(SecurityExternalId.Isin))]
	[Ignore(FieldName = nameof(SecurityExternalId.Ric))]
	[Ignore(FieldName = nameof(SecurityExternalId.Bloomberg))]
	[Ignore(FieldName = nameof(SecurityExternalId.IQFeed))]
	[Ignore(FieldName = nameof(SecurityExternalId.InteractiveBrokers))]
	[Ignore(FieldName = nameof(SecurityExternalId.Plaza))]
	#endregion
	[DisplayNameLoc(LocalizedStrings.IndexKey)]
	[DescriptionLoc(LocalizedStrings.Str728Key)]
	[BasketCode("EI")]
	public class ExpressionIndexSecurity : IndexSecurity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExpressionIndexSecurity"/>.
		/// </summary>
		public ExpressionIndexSecurity()
		{
		}

		/// <summary>
		/// Compiled mathematical formula.
		/// </summary>
		public ExpressionFormula Formula { get; private set; } = ExpressionHelper.CreateError(LocalizedStrings.ExpressionNotSet);

		/// <summary>
		/// The mathematic formula of index.
		/// </summary>
		[Browsable(false)]
		public string Expression
		{
			get => Formula.Expression;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				var service = ConfigManager.TryGetService<ICompilerService>();

				if (service != null)
				{
					Formula = service.Compile(value, true);

					_innerSecurityIds.Clear();

					if (Formula.Error.IsEmpty())
					{
						foreach (var id in Formula.SecurityIds)
						{
							_innerSecurityIds.Add(id.ToSecurityId());
						}
					}
					else
						new InvalidOperationException(Formula.Error).LogError();
				}
				else
					new InvalidOperationException($"Service {nameof(ICompilerService)} is not initialized.").LogError();
			}
		}

		private readonly CachedSynchronizedList<SecurityId> _innerSecurityIds = new CachedSynchronizedList<SecurityId>();

		/// <inheritdoc />
		public override IEnumerable<SecurityId> InnerSecurityIds => _innerSecurityIds.Cache;

		/// <inheritdoc />
		public override Security Clone()
		{
			var clone = new ExpressionIndexSecurity { Expression = Expression };
			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString() => Expression;

		/// <inheritdoc />
		protected override string ToSerializedString()
		{
			return Expression;
		}

		/// <inheritdoc />
		protected override void FromSerializedString(string text)
		{
			Expression = text;
		}
	}
}