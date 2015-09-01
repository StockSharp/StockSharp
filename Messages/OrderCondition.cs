namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Базовое условие заявок (например, параметры стоп- или алго- заявков).
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	public abstract class OrderCondition : Cloneable<OrderCondition>
	{
		/// <summary>
		/// Инициализировать <see cref="OrderCondition"/>.
		/// </summary>
		protected OrderCondition()
		{
		}

		private readonly SynchronizedDictionary<string, object> _parameters = new SynchronizedDictionary<string, object>();

		/// <summary>
		/// Параметры условия.
		/// </summary>
		[Browsable(false)]
		[DataMember]
		public IDictionary<string, object> Parameters
		{
			get { return _parameters; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_parameters.Clear();
				_parameters.AddRange(value);
			}
		}

		/// <summary>
		/// Создать копию <see cref="OrderCondition"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override OrderCondition Clone()
		{
			var clone = GetType().CreateInstance<OrderCondition>();
			clone.Parameters.Clear(); // удаляем параметры по умолчанию
			clone.Parameters.AddRange(_parameters.SyncGet(d => d.ToArray()));
			return clone;
		}
	}
}