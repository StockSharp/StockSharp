namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее изменения.
	/// </summary>
	/// <typeparam name="TField">Тип изменений.</typeparam>
	public abstract class BaseChangeMessage<TField> : Message
	{
		/// <summary>
		/// Серверное время изменения.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ServerTimeKey)]
		[DescriptionLoc(LocalizedStrings.Str168Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		private readonly IDictionary<TField, object> _changes = new Dictionary<TField, object>();

		/// <summary>
		/// Изменения.
		/// </summary>
		[Browsable(false)]
		[DataMember]
		public IDictionary<TField, object> Changes
		{
			get { return _changes; }
		}

		/// <summary>
		/// Инициализировать <see cref="BaseChangeMessage{T}"/>.
		/// </summary>
		/// <param name="type">Тип данных.</param>
		protected BaseChangeMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",T(S)={0:yyyy/MM/dd HH:mm:ss.fff}".Put(ServerTime);
		}
	}
}