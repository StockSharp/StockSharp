namespace StockSharp.Algo.Risk
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Messages;

	/// <summary>
	/// Менеджер контроля рисков.
	/// </summary>
	public class RiskManager : IRiskManager
	{
		/// <summary>
		/// Создать <see cref="RiskManager"/>.
		/// </summary>
		public RiskManager()
		{
		}

		private readonly CachedSynchronizedSet<IRiskRule> _rules = new CachedSynchronizedSet<IRiskRule>();

		/// <summary>
		/// Список правил.
		/// </summary>
		public SynchronizedSet<IRiskRule> Rules
		{
			get { return _rules; }
		}

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		public virtual void Reset()
		{
			_rules.Cache.ForEach(r => r.Reset());
		}

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns>Список правил, которые были активированы сообщением</returns>
		public IEnumerable<IRiskRule> ProcessRules(Message message)
		{
			return _rules.Cache.Where(r => r.ProcessMessage(message)).ToArray();
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public void Load(SettingsStorage storage)
		{
			Rules.AddRange(storage.GetValue<SettingsStorage[]>("Rules").Select(s => s.LoadEntire<IRiskRule>()));
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Rules", Rules.Select(r => r.SaveEntire(false)).ToArray());
		}

		RiskActions IRiskRule.Action
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		string IRiskRule.Title
		{
			get { throw new NotSupportedException(); }
		}

		bool IRiskRule.ProcessMessage(Message message)
		{
			throw new NotSupportedException();
		}
	}
}