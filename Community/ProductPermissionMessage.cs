namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Product permission message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ProductPermissionMessage : BaseSubscriptionIdMessage<ProductPermissionMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProductPermissionMessage"/>.
		/// </summary>
		public ProductPermissionMessage()
			: base(CommunityMessageTypes.ProductPermission)
		{
		}

		/// <summary>
		/// Product.
		/// </summary>
		[DataMember]
		public long ProductId { get; set; }

		/// <summary>
		/// User ID.
		/// </summary>
		[DataMember]
		public long UserId { get; set; }

		/// <summary>
		/// Is manager.
		/// </summary>
		[DataMember]
		public bool IsManager { get; set; }

		/// <summary>
		/// Command.
		/// </summary>
		[DataMember]
		public CommandTypes? Command { get; set; }

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.ProductPermissionType;

		/// <summary>
		/// Create a copy of <see cref="ProductPermissionMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductPermissionMessage
			{
				ProductId = ProductId,
				UserId = UserId,
				Command = Command,
				IsManager = IsManager,
			};
			CopyTo(clone);
			return clone;
		}
	}
}