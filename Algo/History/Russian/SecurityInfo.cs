namespace StockSharp.Algo.History.Russian
{
	using System;

	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Security info.
	/// </summary>
	public class SecurityInfo : Cloneable<SecurityInfo>
	{
		/// <summary>
		/// Security name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Short security name.
		/// </summary>
		public string ShortName { get; set; }
		
		/// <summary>
		/// Security code.
		/// </summary>
		public string Code { get; set; }
		
		/// <summary>
		/// Exchange board where the security is traded.
		/// </summary>
		public string Board { get; set; }
		
		/// <summary>
		/// ID in ISIN format (International Securities Identification Number).
		/// </summary>
		public string Isin { get; set; }
		
		/// <summary>
		/// Underlying asset on which the current security is built.
		/// </summary>
		public string Asset { get; set; }
		
		/// <summary>
		/// Security type.
		/// </summary>
		public string Type { get; set; }
		
		/// <summary>
		/// Number of issued contracts.
		/// </summary>
		public decimal? IssueSize { get; set; }
		
		/// <summary>
		/// Date of issue.
		/// </summary>
		public DateTime? IssueDate { get; set; }
		
		/// <summary>
		/// Security expiration date (for derivatives - expiration, for bonds — redemption).
		/// </summary>
		public DateTime? LastDate { get; set; }
		
		/// <summary>
		/// Number of digits in price after coma.
		/// </summary>
		public int? Decimals { get; set; }
		
		/// <summary>
		/// Lot multiplier.
		/// </summary>
		public decimal? Multiplier { get; set; }
		
		/// <summary>
		/// Minimum price step.
		/// </summary>
		public decimal? PriceStep { get; set; }
		
		/// <summary>
		/// Trading security currency.
		/// </summary>
		public string Currency { get; set; }

		/// <summary>
		/// Settlement date for security (for derivatives and bonds).
		/// </summary>
		public DateTime? SettleDate { get; set; }

		/// <summary>
		/// Get security type.
		/// </summary>
		/// <returns>Security type.</returns>
		public SecurityTypes? GetSecurityType()
		{
			switch (Type)
			{
				case "common_share":
				case "preferred_share":
					return SecurityTypes.Stock;

				case "depositary_receipt":
					return SecurityTypes.Adr;

				case "spread":
				case "futures":
				case "commodity_futures":
					return SecurityTypes.Future;

				case "options":
					return SecurityTypes.Option;

				//case null:
				//case "":
				//	break;

				default:
					return null;
			}
		}

		/// <summary>
		/// To copy fields of the current instrument to <paramref name="security" />.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		public void FillTo(Security security, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			if (security.ShortName.IsEmpty())
				security.ShortName = ShortName;

			security.Name = Name;
			security.Code = Code;

			var board = Board;

			if (board.CompareIgnoreCase("RFUD"))
				board = "FORTS";

			security.Board = exchangeInfoProvider.GetOrCreateBoard(board);

			if (security.Multiplier == null)
				security.Multiplier = Multiplier;

			if (security.Decimals != null)
				security.Decimals = Decimals;

			if (security.PriceStep == null)
			{
				security.PriceStep = PriceStep;

				if (security.Decimals == null && PriceStep != null)
					security.Decimals = PriceStep.Value.GetCachedDecimals();
			}

			if (security.Currency == null)
				security.Currency = Currency.FromMicexCurrencyName();

			if (security.ExternalId.Isin.IsEmpty())
			{
				var externalId = security.ExternalId;
				externalId.Isin = Isin;
				security.ExternalId = externalId;
			}

			if (IssueDate != null)
				security.IssueDate = IssueDate.Value.ApplyTimeZone(TimeHelper.Moscow);

			if (IssueSize != null)
				security.IssueSize = IssueSize;

			if (LastDate != null)
				security.ExpiryDate = LastDate.Value.ApplyTimeZone(TimeHelper.Moscow);

			if (!Asset.IsEmpty())
				security.UnderlyingSecurityId = Asset + "@" + security.Board.Code;

			var secType = GetSecurityType();

			if (secType != null)
				security.Type = secType;
		}

		/// <summary>
		/// To copy fields of the current instrument to <paramref name="security" />.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="securityId">Security ID.</param>
		public void FillTo(SecurityMessage security, ref SecurityId securityId)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (security.ShortName.IsEmpty())
				security.ShortName = ShortName;

			security.Name = Name;
			securityId.SecurityCode = Code;

			var board = Board;

			if (board.CompareIgnoreCase("RFUD"))
				board = "FORTS";

			securityId.BoardCode = board;

			if (security.Multiplier == null)
				security.Multiplier = Multiplier;

			if (security.Decimals != null)
				security.Decimals = Decimals;

			if (security.PriceStep == null)
			{
				security.PriceStep = PriceStep;

				if (security.Decimals == null && PriceStep != null)
					security.Decimals = PriceStep.Value.GetCachedDecimals();
			}

			if (security.Currency == null)
				security.Currency = Currency.FromMicexCurrencyName();

			if (securityId.Isin.IsEmpty())
				securityId.Isin = Isin;

			if (IssueDate != null)
				security.IssueDate = IssueDate.Value.ApplyTimeZone(TimeHelper.Moscow);

			if (IssueSize != null)
				security.IssueSize = IssueSize;

			if (LastDate != null)
				security.ExpiryDate = LastDate.Value.ApplyTimeZone(TimeHelper.Moscow);

			if (!Asset.IsEmpty())
				security.UnderlyingSecurityCode = Asset;

			var secType = GetSecurityType();

			if (secType != null)
				security.SecurityType = secType;
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityInfo"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override SecurityInfo Clone()
		{
			return new SecurityInfo
			{
				Name = Name,
				ShortName = ShortName,
				Code = Code,
				Board = Board,
				Isin = Isin,
				Asset = Asset,
				Type = Type,
				Decimals = Decimals,
				Multiplier = Multiplier,
				PriceStep = PriceStep,
				Currency = Currency,
				IssueSize = IssueSize,
				IssueDate = IssueDate,
				LastDate = LastDate
			};
		}
	}
}