namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Community;
	using StockSharp.Messages;

	/// <summary>
	/// The message contains information about strategy.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyInfoMessage : BaseSubscriptionIdMessage<StrategyInfoMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyInfoMessage"/>.
		/// </summary>
		public StrategyInfoMessage()
			: base(ExtendedMessageTypes.StrategyInfo)
		{
		}

		/// <summary>
		/// Strategy server ID.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Product ID.
		/// </summary>
		[DataMember]
		public long ProductId { get; set; }

		/// <summary>
		/// Strategy ID.
		/// </summary>
		[DataMember]
		public Guid StrategyId { get; set; }

		/// <summary>
		/// Strategy name.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Strategy description.
		/// </summary>
		[DataMember]
		[Obsolete]
		public string Description { get; set; }

		/// <summary>
		/// Strategy parameters.
		/// </summary>
		[DataMember]
		public IDictionary<string, Tuple<string, string>> Parameters { get; } = new Dictionary<string, Tuple<string, string>>();

		/// <summary>
		/// The creation date.
		/// </summary>
		[DataMember]
		public DateTimeOffset CreationDate { get; set; }

		/// <summary>
		/// Strategy tags.
		/// </summary>
		[DataMember]
		[Obsolete]
		public string Tags { get; set; }

		/// <summary>
		/// The identifier of a topic in the forum where the strategy is discussed.
		/// </summary>
		[DataMember]
		[Obsolete]
		public long Topic { get; set; }

		/// <summary>
		/// Type of <see cref="Price"/>.
		/// </summary>
		[DataMember]
		[Obsolete]
		public ProductPriceTypes PriceType { get; set; }

		/// <summary>
		/// The purchase price.
		/// </summary>
		[DataMember]
		[Obsolete]
		public decimal Price { get; set; }

		/// <summary>
		/// Type of <see cref="Content"/>.
		/// </summary>
		[DataMember]
		[Obsolete]
		public ProductContentTypes ContentType { get; set; }

		/// <summary>
		/// Content.
		/// </summary>
		[DataMember]
		[Obsolete]
		public long Content { get; set; }

		/// <summary>
		/// The author identifier.
		/// </summary>
		[DataMember]
		[Obsolete]
		public long Author { get; set; }

		/// <summary>
		/// The picture identifier.
		/// </summary>
		[DataMember]
		[Obsolete]
		public long? Picture { get; set; }

		/// <summary>
		/// The content revision.
		/// </summary>
		[DataMember]
		[Obsolete]
		public int Revision { get; set; }

		/// <summary>
		/// Only visible to author.
		/// </summary>
		[DataMember]
		[Obsolete]
		public bool IsPrivate { get; set; }

		/// <summary>
		/// Is colocation available for the strategy.
		/// </summary>
		[DataMember]
		[Obsolete]
		public bool IsColocation { get; set; }

		/// <summary>
		/// Promo price.
		/// </summary>
		[DataMember]
		[Obsolete]
		public decimal? PromoPrice { get; set; }

		/// <summary>
		/// Promo end date.
		/// </summary>
		[DataMember]
		[Obsolete]
		public DateTimeOffset? PromoEnd { get; set; }

		/// <inheritdoc />
		public override DataType DataType => StrategyDataType.Info;

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Id={StrategyId},Name={Name},Params={Parameters.Select(p => $"{p.Key}={p.Value}").JoinComma()}";
		}

		/// <inheritdoc />
		public override void CopyTo(StrategyInfoMessage destination)
		{
			base.CopyTo(destination);

			destination.Id = Id;
			destination.ProductId = ProductId;
			destination.StrategyId = StrategyId;
			destination.Name = Name;
			destination.CreationDate = CreationDate;
#pragma warning disable CS0612 // Type or member is obsolete
			destination.Description = Description;
			destination.Tags = Tags;
			destination.Topic = Topic;
			destination.PriceType = PriceType;
			destination.Price = Price;
			destination.ContentType = ContentType;
			destination.Content = Content;
			destination.Author = Author;
			destination.Picture = Picture;
			destination.Revision = Revision;
			destination.IsPrivate = IsPrivate;
			destination.IsColocation = IsColocation;
			destination.PromoPrice = PromoPrice;
			destination.PromoEnd = PromoEnd;
			destination.Parameters.AddRange(Parameters);
#pragma warning restore CS0612 // Type or member is obsolete
		}
	}
}