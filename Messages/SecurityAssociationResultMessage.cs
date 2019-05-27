namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// Security association result message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityAssociationResultMessage : BaseResultMessage<SecurityAssociationResultMessage>
	{
		/// <summary>
		/// Initialize <see cref="SecurityAssociationResultMessage"/>.
		/// </summary>
		public SecurityAssociationResultMessage()
			: base(MessageTypes.SecurityAssociationResult)
		{
		}

		private IDictionary<SecurityId, PairSet<string, SecurityId>> _associations = new Dictionary<SecurityId, PairSet<string, SecurityId>>();

		/// <summary>
		/// Security associations.
		/// </summary>
		[DataMember]
		public IDictionary<SecurityId, PairSet<string, SecurityId>> Associations
		{
			get => _associations;
			set => _associations = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		protected override void CopyTo(SecurityAssociationResultMessage destination)
		{
			base.CopyTo(destination);
			destination.Associations = Associations.ToDictionary(p => p.Key, p => p.Value.ToPairSet());
		}

		/// <inheritdoc />
		public override string ToString() =>
			base.ToString() + $",Associations={Associations.Select(m => m.ToString()).Join(",")}";
	}
}