namespace StockSharp.Studio.Core
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Text;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo.Strategies;
	using StockSharp.Logging;
	using StockSharp.Xaml.Diagram;
	using StockSharp.Localization;

    public static class StrategyHelper
	{
		public static bool GetIsInteracted(this StrategyContainer strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			return strategy.StrategyInfo.StrategyType != null && strategy.StrategyInfo.StrategyType.GetAttribute<InteractedStrategyAttribute>() != null;
		}

		public static bool GetIsAutoStart(this StrategyContainer strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			return strategy.StrategyInfo.StrategyType != null && strategy.StrategyInfo.StrategyType.GetAttribute<AutoStartAttribute>() != null;
		}

		public static bool GetIsNoEmulation(this StrategyInfo strategyInfo)
		{
			if (strategyInfo == null)
				throw new ArgumentNullException("strategyInfo");

			return strategyInfo.Type == StrategyInfoTypes.Analytics || 
				strategyInfo.StrategyType == null || 
				strategyInfo.StrategyType.GetAttribute<NoEmulationAttribute>() != null;
		}

		public static void SetIsInitialization(this StrategyContainer strategy, bool isInitialization)
		{
			if (strategy.Strategy != null)
				strategy.Strategy.SetIsInitialization(isInitialization);

			strategy.Environment.SetValue("IsInitializationMode", isInitialization);
		}

		public static void InitStrategy(this StrategyContainer container)
        {
            var info = container.StrategyInfo;

            switch (info.Type)
            {
                case StrategyInfoTypes.SourceCode:
				case StrategyInfoTypes.Analytics:
				{
                    if (info.StrategyType != null)
                        container.Strategy = info.StrategyType.CreateInstance<Strategy>();

                    break;
                }

                case StrategyInfoTypes.Diagram:
	            {
					GuiDispatcher.GlobalDispatcher.AddAction(() =>
					{
						var strategy = (DiagramStrategy)container.Strategy;

						if (strategy == null)
							container.Strategy = strategy = new DiagramStrategy();

						try
						{
							strategy.Composition = ConfigManager.GetService<CompositionRegistry>().Deserialize(info.Body.LoadSettingsStorage());
						}
						catch (Exception ex)
						{
							strategy.AddErrorLog(LocalizedStrings.Str3175Params, ex);
						}
					});
                    
					break;
                }

				case StrategyInfoTypes.Terminal:
                case StrategyInfoTypes.Assembly:
                {
                    if (info.StrategyType != null)
                        container.Strategy = info.StrategyType.CreateInstance<Strategy>();

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void SafeLoadState(this Strategy strategy, SettingsStorage storage)
        {
            // если пользовательский код загрузки был написан с ошибками
            try
            {
                strategy.LoadState(storage);
            }
            catch (Exception ex)
            {
                ex.LogError();
            }
        }

        public static bool IsDiagramStrategy(this StrategyContainer container)
        {
            return container.StrategyInfo.Type == StrategyInfoTypes.Diagram;
        }

		public static Guid GetStrategyId(this StrategyContainer strategyContainer)
		{
			var container = strategyContainer.Strategy as StrategyContainer;

			return container == null ? strategyContainer.Strategy.Id : container.GetStrategyId();
		}
	}
}
