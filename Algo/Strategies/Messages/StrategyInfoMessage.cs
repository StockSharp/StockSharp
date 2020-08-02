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
		public string Tags { get; set; }

		/// <summary>
		/// The identifier of a topic in the forum where the strategy is discussed.
		/// </summary>
		[DataMember]
		public long Topic { get; set; }

		/// <summary>
		/// Type of <see cref="Price"/>.
		/// </summary>
		[DataMember]
		public ProductPriceTypes PriceType { get; set; }

		/// <summary>
		/// The purchase price.
		/// </summary>
		[DataMember]
		public decimal Price { get; set; }

		/// <summary>
		/// Type of <see cref="Content"/>.
		/// </summary>
		[DataMember]
		public ProductContentTypes ContentType { get; set; }

		/// <summary>
		/// Content.
		/// </summary>
		[DataMember]
		public long Content { get; set; }

		/// <summary>
		/// The author identifier.
		/// </summary>
		[DataMember]
		public long Author { get; set; }

		/// <summary>
		/// The picture identifier.
		/// </summary>
		[DataMember]
		public long? Picture { get; set; }

		/// <summary>
		/// The content revision.
		/// </summary>
		[DataMember]
		public int Revision { get; set; }

		/// <summary>
		/// Only visible to author.
		/// </summary>
		[DataMember]
		public bool IsPrivate { get; set; }

		/// <summary>
		/// Is colocation available for the strategy.
		/// </summary>
		[DataMember]
		public bool IsColocation { get; set; }

		/// <summary>
		/// Promo price.
		/// </summary>
		[DataMember]
		public decimal? PromoPrice { get; set; }

		/// <summary>
		/// Promo end date.
		/// </summary>
		[DataMember]
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
			destination.StrategyId = StrategyId;
			destination.Name = Name;
			destination.Description = Description;
			destination.CreationDate = CreationDate;
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
		}
	}
}