namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ecng.Serialization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

using StockSharp.Samples.Strategies.LiveTerminal.Avalonia;

[TestClass]
public class TerminalStrategyPersistenceTests
{
	public sealed class TrackingStrategy : Strategy
	{
		public static int DisposeCount;

		protected override void DisposeManaged()
		{
			Interlocked.Increment(ref DisposeCount);
			base.DisposeManaged();
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void LoadRestoresSecurityAndPortfolioThroughLocalResolvers()
	{
		var persistedSecurity = new Security { Id = "AAPL@NASDAQ" };
		var persistedPortfolio = new Portfolio { Name = "TEST" };
		using var source = new TrackingStrategy
		{
			Security = persistedSecurity,
			Portfolio = persistedPortfolio,
		};
		var storage = source.SaveEntire(false);

		var resolvedSecurity = new Security { Id = persistedSecurity.Id };
		var resolvedPortfolio = new Portfolio { Name = persistedPortfolio.Name };
		var securityCalls = 0;
		var portfolioCalls = 0;

		using var restored = TerminalStrategyPersistence.Load(
			storage,
			id =>
			{
				securityCalls++;
				Assert.AreEqual(persistedSecurity.Id, id);
				return resolvedSecurity;
			},
			name =>
			{
				portfolioCalls++;
				Assert.AreEqual(persistedPortfolio.Name, name);
				return resolvedPortfolio;
			});

		Assert.AreSame(resolvedSecurity, restored.Security);
		Assert.AreSame(resolvedPortfolio, restored.Portfolio);
		Assert.AreEqual(1, securityCalls);
		Assert.AreEqual(1, portfolioCalls);
	}

	[TestMethod]
	[Timeout(10_000)]
	public void LoadDisposesPartiallyRestoredStrategyWhenEntityIsUnavailable()
	{
		TrackingStrategy.DisposeCount = 0;
		using var source = new TrackingStrategy
		{
			Security = new Security { Id = "MISSING@TEST" },
		};
		var storage = source.SaveEntire(false);

		Assert.ThrowsExactly<InvalidOperationException>(() =>
			TerminalStrategyPersistence.Load(storage, _ => null, _ => null));

		Assert.AreEqual(1, TrackingStrategy.DisposeCount);
	}
}
