#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderCondition.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			get => _parameters;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

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
			clone.Parameters.Clear(); // removing pre-defined values
			clone.Parameters.AddRange(_parameters.SyncGet(d => d.Select(p => new KeyValuePair<string, object>(p.Key, p.Value is ICloneable cl ? cl.Clone() : (p.Value is IPersistable pers ? pers.Clone() : p.Value))).ToArray()));
			return clone;
		}
	}
}