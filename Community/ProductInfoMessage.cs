namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Product info message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ProductInfoMessage : BaseSubscriptionIdMessage<ProductInfoMessage>
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Name.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Description.
		/// </summary>
		[DataMember]
		public string Description { get; set; }

		/// <summary>
		/// Package id.
		/// </summary>
		[DataMember]
		public string PackageId { get; set; }

		/// <summary>
		/// Tags.
		/// </summary>
		[DataMember]
		public string Tags { get; set; }

		/// <summary>
		/// Author.
		/// </summary>
		[DataMember]
		public long Author { get; set; }

		/// <summary>
		/// Price.
		/// </summary>
		[DataMember]
		public Currency Price { get; set; }

		/// <summary>
		/// Download count.
		/// </summary>
		[DataMember]
		public int DownloadCount { get; set; }

		/// <summary>
		/// Rating.
		/// </summary>
		[DataMember]
		public decimal? Rating { get; set; }

		/// <summary>
		/// Internet address of help site.
		/// </summary>
		[DataMember]
		public string DocUrl { get; set; }

		/// <summary>
		/// Product required connectors.
		/// </summary>
		[DataMember]
		public bool IsRequiredConnectors { get; set; }

		/// <summary>
		/// Product required connectors.
		/// </summary>
		[DataMember]
		public ProductContentTypes ContentType { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductInfoMessage"/>.
		/// </summary>
		public ProductInfoMessage()
			: base(CommunityMessageTypes.ProductInfo)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.ProductInfoType;

		/// <summary>
		/// Create a copy of <see cref="ProductInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductInfoMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public override void CopyTo(ProductInfoMessage destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			base.CopyTo(destination);

			destination.Id = Id;
			destination.Name = Name;
			destination.Description = Description;
			destination.PackageId = PackageId;
			destination.Tags = Tags;
			destination.Author = Author;
			destination.Price = Price?.Clone();
			destination.DownloadCount = DownloadCount;
			destination.Rating = Rating;
			destination.DocUrl = DocUrl;
			destination.IsRequiredConnectors = IsRequiredConnectors;
			destination.ContentType = ContentType;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString();

			if (Id != 0)
				str += $",Id={Id}";

			if (!Name.IsEmpty())
				str += $",Name={Name}";

			if (!Description.IsEmpty())
				str += $",Descr={Description}";

			if (!PackageId.IsEmpty())
				str += $",PackageId={PackageId}";

			if (!Tags.IsEmpty())
				str += $",Tags={Tags}";

			if (Author != 0)
				str += $",Author={Author}";

			if (Price != null)
				str += $",Price={Price}";

			str += $",Downloads={DownloadCount}";

			if (Rating != null)
				str += $",Rating={Rating}";

			if (!DocUrl.IsEmpty())
				str += $",Doc={DocUrl}";

			str += $",Connectors={IsRequiredConnectors},Content={ContentType}";

			return str;
		}
	}
}