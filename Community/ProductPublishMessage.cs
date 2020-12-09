namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Product publish message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ProductPublishMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProductPublishMessage"/>.
		/// </summary>
		public ProductPublishMessage()
			: base(CommunityMessageTypes.ProductPublish)
		{
		}

		///// <summary>
		///// Product.
		///// </summary>
		//[DataMember]
		//public long ProductId { get; set; }

		private (string packageId, string version, string releaseNotes)[] _packages = ArrayHelper.Empty<(string packageId, string version, string releaseNotes)>();

		/// <summary>
		/// Products.
		/// </summary>
		[DataMember]
		public (string packageId, string version, string releaseNotes)[] Packages
		{
			get => _packages;
			set => _packages = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Create a copy of <see cref="ProductPublishMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductPublishMessage
			{
				//ProductId = ProductId,
				Packages = Packages?.ToArray(),
			};
			CopyTo(clone);
			return clone;
		}
	}
}