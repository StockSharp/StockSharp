namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Base class for messages contains information about the position changes.
	/// </summary>
	[DataContract]
	[Serializable]
	public abstract class BasePositionChangeMessage : BaseChangeMessage<PositionChangeTypes>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected BasePositionChangeMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Portfolio name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[DataMember]
		[MainCategory]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected virtual void CopyTo(BasePositionChangeMessage destination)
		{
			base.CopyTo(destination);

			destination.PortfolioName = PortfolioName;
			destination.ClientCode = ClientCode;
		}
	}
}