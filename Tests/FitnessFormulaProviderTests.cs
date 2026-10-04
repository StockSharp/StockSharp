namespace StockSharp.Tests;

using StockSharp.Algo.Statistics;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;

[TestClass]
public class FitnessFormulaProviderTests : BaseTestClass
{
	private static FitnessFormulaProvider CreateProvider()
		=> new(Helper.FileSystem);

	private async Task<Strategy> CreateStrategyWithStatsAsync(decimal pnl, decimal? recovery = null, decimal? maxDD = null, int? tradeCount = null)
	{
		var strategy = new Strategy();
		var stats = strategy.StatisticManager;

		await stats.SetValueAsync<NetProfitParameter, decimal>(pnl, CancellationToken);

		if (maxDD.HasValue)
			await stats.SetValueAsync<MaxDrawdownParameter, decimal>(maxDD.Value, CancellationToken);

		if (recovery.HasValue)
			await stats.SetValueAsync<RecoveryFactorParameter, decimal>(recovery.Value, CancellationToken);

		if (tradeCount.HasValue)
			await stats.SetValueAsync<TradeCountParameter, int>(tradeCount.Value, CancellationToken);

		return strategy;
	}

	private async Task<Strategy> CreateStrategyWithAllStatsAsync(
		decimal? pnl = null,
		decimal? recovery = null,
		decimal? maxDD = null,
		int? tradeCount = null,
		int? winTrades = null,
		int? losTrades = null)
	{
		var strategy = new Strategy();
		var stats = strategy.StatisticManager;

		if (pnl.HasValue)
			await stats.SetValueAsync<NetProfitParameter, decimal>(pnl.Value, CancellationToken);

		if (maxDD.HasValue)
			await stats.SetValueAsync<MaxDrawdownParameter, decimal>(maxDD.Value, CancellationToken);

		if (recovery.HasValue)
			await stats.SetValueAsync<RecoveryFactorParameter, decimal>(recovery.Value, CancellationToken);

		if (tradeCount.HasValue)
			await stats.SetValueAsync<TradeCountParameter, int>(tradeCount.Value, CancellationToken);

		if (winTrades.HasValue)
			await stats.SetValueAsync<WinningTradesParameter, int>(winTrades.Value, CancellationToken);

		if (losTrades.HasValue)
			await stats.SetValueAsync<LossingTradesParameter, int>(losTrades.Value, CancellationToken);

		return strategy;
	}

	[TestMethod]
	public async Task Compile_SimpleFormula_ReturnsFunction()
	{
		var provider = CreateProvider();

		var fitness = await provider.CompileAsync("PnL", CancellationToken);

		IsNotNull(fitness);
	}

	[TestMethod]
	public async Task Compile_SimpleFormula_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 1000m);

		var result = fitness(strategy);

		result.AssertEqual(1000m);
	}

	[TestMethod]
	public async Task Compile_FormulaWithMultiplication_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL * 2", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 1000m);

		var result = fitness(strategy);

		result.AssertEqual(2000m);
	}

	[TestMethod]
	public async Task Compile_FormulaWithDivision_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL / TCount", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 1000m, tradeCount: 10);

		var result = fitness(strategy);

		result.AssertEqual(100m);
	}

	[TestMethod]
	public async Task Compile_FormulaWithSubtraction_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL - MaxDD", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 1000m, maxDD: 200m);

		var result = fitness(strategy);

		result.AssertEqual(800m);
	}

	[TestMethod]
	public async Task Compile_EmptyFormula_ThrowsArgumentNullException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<ArgumentNullException>(() => provider.CompileAsync("", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_NullFormula_ThrowsArgumentNullException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<ArgumentNullException>(() => provider.CompileAsync(null, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_UnknownVariable_ThrowsArgumentOutOfRangeException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => provider.CompileAsync("UnknownVar", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_InvalidSyntax_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<InvalidOperationException>(() => provider.CompileAsync("PnL +* Recovery", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_UnbalancedParentheses_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<InvalidOperationException>(() => provider.CompileAsync("(PnL + Recovery", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_DivisionByZero_ThrowsAtEvaluation()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL / TCount", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 1000m, tradeCount: 0);

		// Division by zero should throw at evaluation time
		ThrowsExactly<DivideByZeroException>(() => fitness(strategy));
	}

	[TestMethod]
	public async Task Compile_NegativeValues_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: -500m);

		var result = fitness(strategy);

		result.AssertEqual(-500m);
	}

	[TestMethod]
	public async Task Compile_ComplexExpression_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("(PnL + MaxDD) * 2", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m, maxDD: 50m);

		var result = fitness(strategy);

		result.AssertEqual(300m);
	}

	// ========== Real-world fitness formulas ==========

	[TestMethod]
	public async Task Compile_RiskAdjustedReturn_EvaluatesCorrectly()
	{
		// PnL / MaxDD - common risk-adjusted metric
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL / MaxDD", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 1000m, maxDD: 200m);

		var result = fitness(strategy);

		result.AssertEqual(5m);
	}

	[TestMethod]
	public async Task Compile_ProfitFactor_EvaluatesCorrectly()
	{
		// WinTrades / LosTrades - profit factor approximation
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("WinTrades / LosTrades", CancellationToken);
		var strategy = await CreateStrategyWithAllStatsAsync(winTrades: 60, losTrades: 40);

		var result = fitness(strategy);

		result.AssertEqual(1.5m);
	}

	[TestMethod]
	public async Task Compile_WeightedPnLByTrades_EvaluatesCorrectly()
	{
		// PnL * TCount / 100 - weighted by trade activity
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL * TCount / 100", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 500m, tradeCount: 20);

		var result = fitness(strategy);

		result.AssertEqual(100m);
	}

	[TestMethod]
	public async Task Compile_PnLWithPenalty_EvaluatesCorrectly()
	{
		// PnL * 2 - MaxDD - combines pnl with drawdown penalty
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL * 2 - MaxDD", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 1000m, maxDD: 500m);

		var result = fitness(strategy);

		result.AssertEqual(1500m); // 2 * 1000 - 500
	}

	// ========== Recovery variable tests ==========

	[TestMethod]
	public async Task Compile_RecoveryTimePnL_EvaluatesCorrectly()
	{
		// Recovery * PnL - tests Recovery variable
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("Recovery * PnL", CancellationToken);
		var strategy = await CreateStrategyWithAllStatsAsync(pnl: 1000m, recovery: 2m, maxDD: 500m);

		var result = fitness(strategy);

		result.AssertEqual(2000m); // 2 * 1000
	}

	[TestMethod]
	public async Task Compile_RecoveryWithPenalty_EvaluatesCorrectly()
	{
		// Recovery * PnL - MaxDD - combines recovery with drawdown penalty
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("Recovery * PnL - MaxDD", CancellationToken);
		var strategy = await CreateStrategyWithAllStatsAsync(pnl: 1000m, recovery: 2m, maxDD: 500m);

		var result = fitness(strategy);

		result.AssertEqual(1500m); // 2 * 1000 - 500
	}

	[TestMethod]
	public async Task Compile_RecoveryOnly_EvaluatesCorrectly()
	{
		// Just Recovery variable
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("Recovery", CancellationToken);
		var strategy = await CreateStrategyWithAllStatsAsync(pnl: 800m, maxDD: 400m, recovery: 2m);

		var result = fitness(strategy);

		result.AssertEqual(2m); // Recovery = PnL / MaxDD = 800 / 400 = 2
	}

	[TestMethod]
	public async Task Compile_RecoveryInComplexFormula_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("(PnL + Recovery) * 2", CancellationToken);
		var strategy = await CreateStrategyWithAllStatsAsync(pnl: 100m, recovery: 50m, maxDD: 200m);

		var result = fitness(strategy);

		result.AssertEqual(300m); // (100 + 50) * 2
	}

	// ========== Complex nested expressions ==========

	[TestMethod]
	public async Task Compile_NestedParentheses_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("((PnL + MaxDD) * (TCount - 5)) / 10", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m, maxDD: 50m, tradeCount: 15);

		var result = fitness(strategy);

		result.AssertEqual(150m); // ((100 + 50) * (15 - 5)) / 10 = 150 * 10 / 10 = 150
	}

	[TestMethod]
	public async Task Compile_DeeplyNestedParentheses_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("(((PnL)))", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 777m);

		var result = fitness(strategy);

		result.AssertEqual(777m);
	}

	[TestMethod]
	public async Task Compile_ComplexMultiVariable_EvaluatesCorrectly()
	{
		// (PnL * 3) / (MaxDD + 1)
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("(PnL * 3) / (MaxDD + 1)", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 500m, maxDD: 499m);

		var result = fitness(strategy);

		result.AssertEqual(3m); // (500 * 3) / (499 + 1) = 1500 / 500 = 3
	}

	// ========== Order of operations ==========

	[TestMethod]
	public async Task Compile_OrderOfOperations_MultiplicationBeforeAddition()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL + MaxDD * TCount", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m, maxDD: 10m, tradeCount: 5);

		var result = fitness(strategy);

		result.AssertEqual(150m); // 100 + (10 * 5) = 150, not (100 + 10) * 5 = 550
	}

	[TestMethod]
	public async Task Compile_OrderOfOperations_DivisionBeforeSubtraction()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL - MaxDD / TCount", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m, maxDD: 50m, tradeCount: 10);

		var result = fitness(strategy);

		result.AssertEqual(95m); // 100 - (50 / 10) = 100 - 5 = 95
	}

	[TestMethod]
	public async Task Compile_OrderOfOperations_ParenthesesOverride()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("(PnL + MaxDD) * TCount", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m, maxDD: 10m, tradeCount: 5);

		var result = fitness(strategy);

		result.AssertEqual(550m); // (100 + 10) * 5 = 550
	}

	// ========== Edge cases with numbers ==========

	[TestMethod]
	public async Task Compile_VeryLargeNumbers_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL * 1000", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 1_000_000_000m);

		var result = fitness(strategy);

		result.AssertEqual(1_000_000_000_000m);
	}

	[TestMethod]
	public async Task Compile_VerySmallNumbers_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL / TCount", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 0.001m, tradeCount: 1000);

		var result = fitness(strategy);

		result.AssertEqual(0.000001m);
	}

	[TestMethod]
	public async Task Compile_ZeroValue_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL * MaxDD", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 0m, maxDD: 100m);

		var result = fitness(strategy);

		result.AssertEqual(0m);
	}

	[TestMethod]
	public async Task Compile_NegativePnlAndZeroTradeCount_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL + TCount", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: -100m, tradeCount: 0);

		var result = fitness(strategy);

		result.AssertEqual(-100m);
	}

	[TestMethod]
	public async Task Compile_MixedPositiveNegative_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL - MaxDD", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: -100m, maxDD: 50m);

		var result = fitness(strategy);

		result.AssertEqual(-150m); // -100 - 50 = -150
	}

	// ========== Formulas with literals/constants ==========

	[TestMethod]
	public async Task Compile_FormulaWithConstant_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL * 2", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(200m);
	}

	[TestMethod]
	public async Task Compile_FormulaWithDecimalConstant_EvaluatesCorrectly()
	{
		// Decimal literals are auto-converted (0.5 → 0.5m) by ExpressionHelper
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL * 0.5", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(50m);
	}

	[TestMethod]
	public async Task Compile_FormulaWithNegativeConstant_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL + (-100)", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 250m);

		var result = fitness(strategy);

		result.AssertEqual(150m);
	}

	[TestMethod]
	public async Task Compile_ComplexWithMultipleConstants_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("(PnL - 50) * 2 + 100", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(200m); // (100 - 50) * 2 + 100 = 50 * 2 + 100 = 200
	}

	// ========== All variables combinations ==========

	[TestMethod]
	public async Task Compile_AllFourArithmeticOperators_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL + 50 - MaxDD * TCount / 10", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m, maxDD: 20m, tradeCount: 10);

		var result = fitness(strategy);

		// 100 + 50 - (20 * 10 / 10) = 100 + 50 - 20 = 130
		result.AssertEqual(130m);
	}

	[TestMethod]
	public async Task Compile_MultipleVariablesSameType_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("WinTrades - LosTrades", CancellationToken);
		var strategy = await CreateStrategyWithAllStatsAsync(winTrades: 70, losTrades: 30);

		var result = fitness(strategy);

		result.AssertEqual(40m);
	}

	// ========== Error cases - more variations ==========

	[TestMethod]
	public async Task Compile_EmptyParentheses_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<InvalidOperationException>(() => provider.CompileAsync("PnL + ()", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_MissingOperator_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<InvalidOperationException>(() => provider.CompileAsync("PnL Recovery", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_TrailingOperator_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<InvalidOperationException>(() => provider.CompileAsync("PnL +", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_LeadingOperator_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<InvalidOperationException>(() => provider.CompileAsync("* PnL", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_DoubleOperator_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<InvalidOperationException>(() => provider.CompileAsync("PnL ++ Recovery", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_WhitespaceOnly()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<ArgumentException>(() => provider.CompileAsync("   ", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_TabsOnly()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<ArgumentException>(() => provider.CompileAsync("\t\t", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_NewlinesOnly()
	{
		var provider = CreateProvider();

		await ThrowsExactlyAsync<ArgumentException>(() => provider.CompileAsync("\n\n", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Compile_CaseInsensitiveVariable_EvaluatesCorrectly()
	{
		// Variables are case-insensitive
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("pnl", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(100m);
	}

	[TestMethod]
	public async Task Compile_MixedCaseVariable_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PNL", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(100m);
	}

	// ========== More decimal literal edge cases ==========

	[TestMethod]
	public async Task Compile_DivisionByDecimal_EvaluatesCorrectly()
	{
		// Decimal literals are auto-converted (0.5 → 0.5m) by ExpressionHelper
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL / 0.5", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(200m); // 100 / 0.5 = 200
	}

	[TestMethod]
	public async Task Compile_AdditionWithDecimal_EvaluatesCorrectly()
	{
		// Decimal literals are auto-converted (0.1 → 0.1m) by ExpressionHelper
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL + 0.1", CancellationToken);
		var strategy = await CreateStrategyWithStatsAsync(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(100.1m);
	}

	// ========== Order of operations with Recovery ==========

	[TestMethod]
	public async Task Compile_OrderOfOperations_RecoveryMultiplicationBeforeAddition()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("PnL + Recovery * TCount", CancellationToken);
		var strategy = await CreateStrategyWithAllStatsAsync(pnl: 100m, recovery: 10m, maxDD: 100m, tradeCount: 5);

		var result = fitness(strategy);

		result.AssertEqual(150m); // 100 + (10 * 5) = 150
	}

	[TestMethod]
	public async Task Compile_OrderOfOperations_RecoveryParenthesesOverride()
	{
		var provider = CreateProvider();
		var fitness = await provider.CompileAsync("(PnL + Recovery) * TCount", CancellationToken);
		var strategy = await CreateStrategyWithAllStatsAsync(pnl: 100m, recovery: 10m, maxDD: 100m, tradeCount: 5);

		var result = fitness(strategy);

		result.AssertEqual(550m); // (100 + 10) * 5 = 550
	}
}

file static class StatisticManagerTestExtensions
{
	public static async Task SetValueAsync<TParam, TValue>(this IStatisticManager stats, TValue value, CancellationToken cancellationToken)
		where TParam : IStatisticParameter<TValue>
		where TValue : IComparable<TValue>
	{
		var param = stats.Parameters.OfType<TParam>().First();
		await param.LoadAsync(new SettingsStorage { { nameof(IStatisticParameter<TValue>.Value), value } }, cancellationToken);
	}
}
