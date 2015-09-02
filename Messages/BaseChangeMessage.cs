namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// A message containing changes.
	/// </summary>
	/// <typeparam name="TField">Changes type.</typeparam>
	[DataContract]
	[Serializable]
	public abstract class BaseChangeMessage<TField> : Message
	{
		/// <summary>
		/// Change server time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ServerTimeKey)]
		[DescriptionLoc(LocalizedStrings.Str168Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		private readonly IDictionary<TField, object> _changes = new Dictionary<TField, object>();

		/// <summary>
		/// Changes.
		/// </summary>
		[Browsable(false)]
		[DataMember]
		public IDictionary<TField, object> Changes
		{
			get { return _changes; }
		}

		/// <summary>
		/// Initialize <see cref="BaseChangeMessage{T}"/>.
		/// </summary>
		/// <param name="type">Data type.</param>
		protected BaseChangeMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",T(S)={0:yyyy/MM/dd HH:mm:ss.fff}".Put(ServerTime);
		}
	}
}