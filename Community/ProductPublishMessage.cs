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

		private (long, string, string, string)[] _packages = ArrayHelper.Empty<(long, string, string, string)>();

		/// <summary>
		/// Products.
		/// </summary>
		[DataMember]
		public (long productId, string version, string releaseNotesEn, string releaseNotesRu)[] Packages
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
				Packages = Packages.ToArray(),
			};
			CopyTo(clone);
			return clone;
		}
	}
}