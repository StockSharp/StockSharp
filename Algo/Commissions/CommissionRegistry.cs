namespace StockSharp.Algo.Commissions
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Community;

	/// <summary>
	/// Реестр всех доступных тарифных планов, получающий данные с сервера StockSharp.
	/// </summary>
	public sealed class CommissionRegistry : BaseCommunityClient<ICommissionService>
	{
		private readonly SynchronizedDictionary<string, CommissionRule[]> _profiles = new SynchronizedDictionary<string, CommissionRule[]>();

		/// <summary>
		/// Создать <see cref="CommissionRegistry"/>.
		/// </summary>
		public CommissionRegistry()
			: this("http://stocksharp.com/services/commissionservice.svc".To<Uri>())
		{
		}

		/// <summary>
		/// Создать <see cref="CommissionRegistry"/>.
		/// </summary>
		/// <param name="address">Адрес сервера.</param>
		public CommissionRegistry(Uri address)
			: base(address, "commission")
		{
		}

		private string[] _names;

		/// <summary>
		/// Все названия тарифных планов.
		/// </summary>
		public IEnumerable<string> Names
		{
			get { return _names ?? (_names = Invoke(s => s.GetNames(SessionId))); }
		}

		/// <summary>
		/// Получить тарифный план по его имени.
		/// </summary>
		/// <param name="name">Название тарифного плана.</param>
		/// <returns>Тарифный план.</returns>
		public CommissionRule[] Get(string name)
		{
			return _profiles.SafeAdd(name, key => Invoke(s => s.GetRules(SessionId, key)));
		}
	}
}