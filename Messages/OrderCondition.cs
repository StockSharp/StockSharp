namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Базовое условие заявок (например, параметры стоп- или алго- заявков).
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
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

		///// <summary>
		///// Проверить, возможно ли по рыночной цене активировать заявку. И возвратить демо заявки, если это возможно.
		///// </summary>
		///// <param name="depth">Стакан, отражающий текущую рыночную ситуацию.</param>
		///// <returns>Демо заявки.</returns>
		//public virtual IEnumerable<Order> TryActivate(MarketDepth depth)
		//{
		//	throw new NotImplementedException();
		//}

		/// <summary>
		/// Создать копию условия (копирование параметров условия).
		/// </summary>
		/// <returns>Копия условия.</returns>
		public override OrderCondition Clone()
		{
			var clone = GetType().CreateInstance<OrderCondition>();
			clone.Parameters.Clear(); // удаляем параметры по умолчанию
			clone.Parameters.AddRange(_parameters.SyncGet(d => d.ToArray()));
			return clone;
		}
	}
}