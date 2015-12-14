#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: StrategyInfoHolder.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Ribbon
{
	using System;

	using Ecng.Common;

	using StockSharp.Studio.Core;

	class StrategyInfoHolder
	{
		public event Action StrategiesUpdated;

		public event Action StrategyInfosUpdated;

		public void Set(StrategyInfo oldValue, StrategyInfo newValue)
		{
			if (oldValue != null)
				UnSubscribeStrategiesCollectionChanged(oldValue);

			if (newValue != null)
				SubscribeStrategiesCollectionChanged(newValue);

			StrategiesCollectionChanged();
		}

		private void SubscribeStrategiesCollectionChanged(StrategyInfo info)
		{
			info.Strategies.Added += StrategyAdded;
			info.Strategies.Removed += StrategyRemoved;
			info.Strategies.Cleared += StrategiesCollectionChanged;
		}

		private void UnSubscribeStrategiesCollectionChanged(StrategyInfo info)
		{
			info.Strategies.Added -= StrategyAdded;
			info.Strategies.Removed -= StrategyRemoved;
			info.Strategies.Cleared -= StrategiesCollectionChanged;
		}

		private void StrategyRemoved(StrategyContainer strategyContainer)
		{
			StrategiesCollectionChanged();
		}

		private void StrategyAdded(StrategyContainer strategyContainer)
		{
			StrategiesCollectionChanged();
		}

		private void StrategiesCollectionChanged()
		{
			StrategiesUpdated.SafeInvoke();
		}

		public void Set(IStudioEntityRegistry registry)
		{
			registry.Strategies.Added += s => StrategyInfosCollectionChanged();
			registry.Strategies.Removed += s => StrategyInfosCollectionChanged();
			registry.Strategies.Cleared += StrategyInfosCollectionChanged;

			StrategyInfosCollectionChanged();
		}

		private void StrategyInfosCollectionChanged()
		{
			StrategyInfosUpdated.SafeInvoke();
		}
	}
}