#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StrategyRunner.StrategyRunnerPublic
File: StrategyConnector.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.StrategyRunner
{
	using System.Collections.Generic;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	class StrategyConnector : Connector
	{
		private readonly StrategyEntityFactory _entityFactory;

		public ISecurityProvider SecurityProvider
		{
			get { return _entityFactory.SecurityProvider; }
		}

		public StrategyConnector()
		{
			EntityFactory = _entityFactory = new StrategyEntityFactory();
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		public override IEnumerable<Security> Lookup(Security criteria)
		{
			return _entityFactory.Lookup(criteria);
		}

		public Portfolio LookupPortfolio(string name)
		{
			return _entityFactory.LookupPortfolio(name);
		}
	}
}
