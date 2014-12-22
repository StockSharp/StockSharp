namespace StockSharp.Hydra
{
	using System;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;

	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Localization;

	static class Extensions
	{
		public static HydraTaskSecurity ToTaskSecurity(this IHydraTask task, Security security)
		{
			if (task == null)
				throw new ArgumentNullException("task");

			if (security == null)
				throw new ArgumentNullException("security");

			return new HydraTaskSecurity
			{
				MarketDataTypes = task.SupportedMarketDataTypes.ToArray(),
				CandleSeries = task.SupportedCandleSeries.Select(s => s.Clone()).ToArray(),
				Settings = task.Settings,
				Security = security,
			};
		}

		public static Task ContinueWithExceptionHandling(this Task task, Window window, Action<bool> action)
		{
			if (task == null)
				throw new ArgumentNullException("task");

			if (window == null)
				throw new ArgumentNullException("window");

			if (action == null)
				throw new ArgumentNullException("action");

			return task.ContinueWith(t =>
			{
				if (task.IsFaulted)
				{
					Exception ex;

					if (task.Exception != null)
					{
						task.Exception.LogError();
						ex = task.Exception.InnerException;
					}
					else
					{
						ex = new InvalidOperationException(LocalizedStrings.Str2914);
						ex.LogError();
					}

					new MessageBoxBuilder()
							.Caption(LocalizedStrings.Str2915)
							.Text(ex.ToString())
							.Error()
							.Owner(window)
							.Show();
				}

				action(!task.IsFaulted);
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}
}