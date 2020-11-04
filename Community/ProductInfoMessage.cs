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
		/// Description (en).
		/// </summary>
		[DataMember]
		public string Description { get; set; }

		/// <summary>
		/// Description (ru).
		/// </summary>
		[DataMember]
		public string DescriptionRu { get; set; }

		/// <summary>
		/// Full description (en).
		/// </summary>
		[DataMember]
		public string FullDescriptionEn { get; set; }

		/// <summary>
		/// Full description (ru).
		/// </summary>
		[DataMember]
		public string FullDescriptionRu { get; set; }

		/// <summary>
		/// Package id.
		/// </summary>
		[DataMember]
		public string PackageId { get; set; }

		/// <summary>
		/// Repository.
		/// </summary>
		[DataMember]
		public PackageRepositories Repository { get; set; }

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
		/// Per month.
		/// </summary>
		[DataMember]
		public Currency MonthlyPrice { get; set; }

		/// <summary>
		/// Annual.
		/// </summary>
		[DataMember]
		public Currency AnnualPrice { get; set; }

		/// <summary>
		/// Lifetime.
		/// </summary>
		[DataMember]
		public Currency LifetimePrice { get; set; }

		/// <summary>
		/// Lifetime.
		/// </summary>
		[DataMember]
		[Obsolete]
		public Currency Price
		{
			get => LifetimePrice;
			set => LifetimePrice = value;
		}

		/// <summary>
		/// Price for renew.
		/// </summary>
		[DataMember]
		public Currency RenewPrice { get; set; }

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
		[Obsolete("Use SupportedPlugins property.")]
		public bool IsRequiredConnectors { get; set; }

		/// <summary>
		/// Supported plugins.
		/// </summary>
		[DataMember]
		public long? SupportedPlugins { get; set; }

		/// <summary>
		/// Content type.
		/// </summary>
		[DataMember]
		public ProductContentTypes ContentType { get; set; }

		/// <summary>
		/// The picture identifier.
		/// </summary>
		[DataMember]
		public long Picture { get; set; }

		/// <summary>
		/// Extra.
		/// </summary>
		[DataMember]
		public string Extra { get; set; }

		/// <summary>
		/// Scope.
		/// </summary>
		[DataMember]
		public ProductScopes Scope { get; set; }

		/// <summary>
		/// Type of <see cref="Price"/>.
		/// </summary>
		[DataMember]
		[Obsolete("Use MonthlyPrice, AnnualPrice or LifetimePrice properties.")]
		public ProductPriceTypes PriceType { get; set; }

		/// <summary>
		/// Latest version.
		/// </summary>
		[DataMember]
		public string LatestVersion { get; set; }

		/// <summary>
		/// Is approved.
		/// </summary>
		[DataMember]
		public bool IsApproved { get; set; }

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
			destination.DescriptionRu = DescriptionRu;
			destination.FullDescriptionEn = FullDescriptionEn;
			destination.FullDescriptionRu = FullDescriptionRu;
			destination.PackageId = PackageId;
			destination.Repository = Repository;
			destination.Tags = Tags;
			destination.Author = Author;
			destination.MonthlyPrice = MonthlyPrice?.Clone();
			destination.AnnualPrice = AnnualPrice?.Clone();
			destination.LifetimePrice = LifetimePrice?.Clone();
			destination.RenewPrice = RenewPrice?.Clone();
			destination.DownloadCount = DownloadCount;
			destination.Rating = Rating;
			destination.DocUrl = DocUrl;
#pragma warning disable CS0618 // Type or member is obsolete
			destination.IsRequiredConnectors = IsRequiredConnectors;
			destination.PriceType = PriceType;
#pragma warning restore CS0618 // Type or member is obsolete
			destination.SupportedPlugins = SupportedPlugins;
			destination.ContentType = ContentType;
			destination.Picture = Picture;
			destination.Extra = Extra;
			destination.Scope = Scope;
			destination.LatestVersion = LatestVersion;
			destination.IsApproved = IsApproved;
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
				str += $",PackageId={PackageId},Repo={Repository}";

			if (!Tags.IsEmpty())
				str += $",Tags={Tags}";

			if (Author != 0)
				str += $",Author={Author}";

			if (MonthlyPrice != null)
				str += $",Monthly={MonthlyPrice}";

			if (AnnualPrice != null)
				str += $",Annual={AnnualPrice}";

			if (LifetimePrice != null)
				str += $",Life={LifetimePrice}";

			if (RenewPrice != null)
				str += $",Renew={RenewPrice}";

			str += $",Downloads={DownloadCount}";

			if (Rating != null)
				str += $",Rating={Rating}";

			if (!DocUrl.IsEmpty())
				str += $",Doc={DocUrl}";

			if (Picture != default)
				str += $",Picture={Picture}";

			str += $",Content={ContentType}";

			if (SupportedPlugins != null)
				str += $",Supported={SupportedPlugins.Value}";

			if (!Extra.IsEmpty())
				str += $",Extra={Extra}";

			if (!LatestVersion.IsEmpty())
				str += $",Ver={LatestVersion}";

			if (!IsApproved)
				str += $",Approved={IsApproved}";

			return str;
		}
	}
}