#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: PortfolioMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Portfolio states.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum PortfolioStates
	{
		/// <summary>
		/// Active.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str248Key)]
		Active,
		
		/// <summary>
		/// Blocked.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str249Key)]
		Blocked,
	}

	/// <summary>
	/// The message contains information about portfolio.
	/// </summary>
	[DataContract]
	[Serializable]
	public class PortfolioMessage : Message
	{
		/// <summary>
		/// Portfolio code name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Portfolio currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str251Key)]
		[MainCategory]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey, true)]
		[MainCategory]
		public string BoardCode { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[DataMember]
		[MainCategory]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		///// <summary>
		///// Portfolio state.
		///// </summary>
		//[DataMember]
		//[DisplayNameLoc(LocalizedStrings.StateKey)]
		//[DescriptionLoc(LocalizedStrings.Str252Key)]
		//[MainCategory]
		//public PortfolioStates? State { get; set; }

		/// <summary>
		/// ID of the original message <see cref="TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Subscription/unsubscription portfolio changes transaction id.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// Is the message subscription portfolio changes.
		/// </summary>
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioMessage"/>.
		/// </summary>
		public PortfolioMessage()
			: base(MessageTypes.Portfolio)
		{
		}

		/// <summary>
		/// Initialize <see cref="PortfolioMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected PortfolioMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Name={PortfolioName}";
		}

		/// <summary>
		/// Create a copy of <see cref="PortfolioMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new PortfolioMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected PortfolioMessage CopyTo(PortfolioMessage destination)
		{
			destination.PortfolioName = PortfolioName;
			destination.Currency = Currency;
			destination.BoardCode = BoardCode;
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.IsSubscribe = IsSubscribe;
			//destination.State = State;
			destination.TransactionId = TransactionId;
			destination.ClientCode = ClientCode;

			this.CopyExtensionInfo(destination);

			return destination;
		}
	}
}