namespace StockSharp.Samples.Strategies.LiveTerminal.Avalonia;

using System;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

internal static class TerminalStrategyPersistence
{
	private const string SettingsKey = "settings";

	public static Strategy Load(
		SettingsStorage envelope,
		Func<string, Security> resolveSecurity,
		Func<string, Portfolio> resolvePortfolio)
	{
		ArgumentNullException.ThrowIfNull(envelope);
		ArgumentNullException.ThrowIfNull(resolveSecurity);
		ArgumentNullException.ThrowIfNull(resolvePortfolio);

		var strategy = envelope.LoadEntire<Strategy>();
		try
		{
			var strategyStorage = envelope.GetValue<SettingsStorage>(SettingsKey);
			var parameterStorages = strategyStorage?.GetValue<SettingsStorage[]>(nameof(Strategy.Parameters));

			if (parameterStorages is null)
				return strategy;

			foreach (var parameterStorage in parameterStorages)
			{
				var id = parameterStorage.GetValue<string>(nameof(IStrategyParam.Id));
				if (!strategy.Parameters.TryGetValue(id, out var parameter))
					continue;

				var isSecurity = parameter.Type.Is<Security>();
				var isPortfolio = parameter.Type.Is<Portfolio>();
				if (!isSecurity && !isPortfolio)
					continue;

				var persistedValue = parameterStorage.GetValue(nameof(IStrategyParam.Value), string.Empty);
				if (persistedValue.IsEmpty())
					continue;

				if (isSecurity)
				{
					parameter.Value = resolveSecurity(persistedValue)
						?? throw new InvalidOperationException($"Security '{persistedValue}' is not available for strategy '{strategy.Name}'.");
				}
				else if (parameter.Type.Is<Portfolio>())
				{
					parameter.Value = resolvePortfolio(persistedValue)
						?? throw new InvalidOperationException($"Portfolio '{persistedValue}' is not available for strategy '{strategy.Name}'.");
				}
			}

			return strategy;
		}
		catch
		{
			strategy.Dispose();
			throw;
		}
	}
}
