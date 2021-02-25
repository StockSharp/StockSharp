namespace StockSharp.Community
{
	using StockSharp.Messages;

	/// <summary>
	/// Extended <see cref="MessageTypes"/>.
	/// </summary>
	public static class CommunityMessageTypes
	{
		/// <summary>
		/// <see cref="FileInfoMessage"/>.
		/// </summary>
		public const MessageTypes FileInfo = (MessageTypes)(-11000);

		/// <summary>
		/// <see cref="ProductInfoMessage"/>.
		/// </summary>
		public const MessageTypes ProductInfo = (MessageTypes)(-11001);

		///// <summary>
		///// <see cref="ProductLookupMessage"/>.
		///// </summary>
		//public const MessageTypes ProductLookup = (MessageTypes)(-11002);

		/// <summary>
		/// <see cref="ProductFeedbackMessage"/>.
		/// </summary>
		public const MessageTypes ProductFeedback = (MessageTypes)(-11003);

		///// <summary>
		///// <see cref="ProductFeedbackLookupMessage"/>.
		///// </summary>
		//public const MessageTypes ProductFeedbackLookup = (MessageTypes)(-11004);

		///// <summary>
		///// <see cref="ProductPermissionLookupMessage"/>.
		///// </summary>
		//public const MessageTypes ProductPermissionLookup = (MessageTypes)(-11005);

		/// <summary>
		/// <see cref="ProductPermissionMessage"/>.
		/// </summary>
		public const MessageTypes ProductPermission = (MessageTypes)(-11006);

		/// <summary>
		/// <see cref="ProductPublishMessage"/>.
		/// </summary>
		public const MessageTypes ProductPublish = (MessageTypes)(-11007);

		/// <summary>
		/// <see cref="ProductCategoryMessage"/>.
		/// </summary>
		public const MessageTypes ProductCategory = (MessageTypes)(-11008);

		///// <summary>
		///// <see cref="ProductCategoryLookupMessage"/>.
		///// </summary>
		//public const MessageTypes ProductCategoryLookup = (MessageTypes)(-11009);

		///// <summary>
		///// <see cref="LicenseLookupMessage"/>.
		///// </summary>
		//public const MessageTypes LicenseLookup = (MessageTypes)(-10000);

		/// <summary>
		/// <see cref="LicenseRequestMessage"/>.
		/// </summary>
		public const MessageTypes LicenseRequest = (MessageTypes)(-10001);

		/// <summary>
		/// <see cref="LicenseInfoMessage"/>.
		/// </summary>
		public const MessageTypes LicenseInfo = (MessageTypes)(-10002);

		/// <summary>
		/// <see cref="LicenseFeatureMessage"/>.
		/// </summary>
		public const MessageTypes LicenseFeature = (MessageTypes)(-10003);

		/// <summary>
		/// <see cref="ProductInfoMessage"/>.
		/// </summary>
		public static DataType ProductInfoType = DataType.Create(typeof(ProductInfoMessage), null).Immutable();

		/// <summary>
		/// <see cref="ProductFeedbackMessage"/>.
		/// </summary>
		public static DataType ProductFeedbackType = DataType.Create(typeof(ProductFeedbackMessage), null).Immutable();

		/// <summary>
		/// <see cref="ProductPermissionMessage"/>.
		/// </summary>
		public static DataType ProductPermissionType = DataType.Create(typeof(ProductPermissionMessage), null).Immutable();

		/// <summary>
		/// <see cref="LicenseInfoMessage"/>.
		/// </summary>
		public static DataType LicenseInfoType = DataType.Create(typeof(LicenseInfoMessage), null).Immutable();
		
		/// <summary>
		/// <see cref="LicenseFeatureMessage"/>.
		/// </summary>
		public static DataType LicenseFeatureType = DataType.Create(typeof(LicenseFeatureMessage), null).Immutable();

		/// <summary>
		/// <see cref="ProductCategoryMessage"/>.
		/// </summary>
		public static DataType ProductCategoryType = DataType.Create(typeof(ProductCategoryMessage), null).Immutable();
	}
}