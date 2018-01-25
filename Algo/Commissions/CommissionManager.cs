#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Commissions.Algo
File: CommissionManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Commissions
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Messages;

	/// <summary>
	/// The commission calculating manager.
	/// </summary>
	public class CommissionManager : ICommissionManager
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommissionManager"/>.
		/// </summary>
		public CommissionManager()
		{
		}

		private readonly CachedSynchronizedSet<ICommissionRule> _rules = new CachedSynchronizedSet<ICommissionRule>();

		/// <summary>
		/// The list of commission calculating rules.
		/// </summary>
		public ISynchronizedCollection<ICommissionRule> Rules => _rules;

		/// <summary>
		/// Total commission.
		/// </summary>
		public virtual decimal Commission { get; private set; }

		/// <summary>
		/// To reset the state.
		/// </summary>
		public virtual void Reset()
		{
			Commission = 0;
			_rules.Cache.ForEach(r => r.Reset());
		}

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		public virtual decimal? Process(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					Reset();
					return null;
				}
				case MessageTypes.Execution:
				{
					if (_rules.Count == 0)
						return null;

					decimal? commission = null;

					foreach (var rule in _rules.Cache)
					{
						var ruleCom = rule.Process(message);

						if (ruleCom != null)
						{
							if (commission == null)
								commission = 0;

							commission += ruleCom.Value;
						}
					}

					if (commission != null)
						Commission += commission.Value;

					return commission;
				}
				default:
					return null;
			}
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Load(SettingsStorage storage)
		{
			Rules.AddRange(storage.GetValue<SettingsStorage[]>(nameof(Rules)).Select(s => s.LoadEntire<ICommissionRule>()));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Rules), Rules.Select(r => r.SaveEntire(false)).ToArray());
		}

		string ICommissionRule.Title => throw new NotSupportedException();

		Unit ICommissionRule.Value => throw new NotSupportedException();
	}
}