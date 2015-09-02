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
	/// Base order condition (for example, for stop order algo orders).
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	public abstract class OrderCondition : Cloneable<OrderCondition>
	{
		/// <summary>
		/// Initialize <see cref="OrderCondition"/>.
		/// </summary>
		protected OrderCondition()
		{
		}

		private readonly SynchronizedDictionary<string, object> _parameters = new SynchronizedDictionary<string, object>();

		/// <summary>
		/// Condition parameters.
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
		/// Create a copy of <see cref="OrderCondition"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override OrderCondition Clone()
		{
			var clone = GetType().CreateInstance<OrderCondition>();
			clone.Parameters.Clear(); // удаляем параметры по умолчанию
			clone.Parameters.AddRange(_parameters.SyncGet(d => d.ToArray()));
			return clone;
		}
	}
}