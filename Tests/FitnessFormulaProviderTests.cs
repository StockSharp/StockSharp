namespace StockSharp.Tests;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Statistics;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;

[TestClass]
public class FitnessFormulaProviderTests : BaseTestClass
{
	private static FitnessFormulaProvider CreateProvider()
		=> new(Helper.FileSystem);

	private static Strategy CreateStrategyWithStats(decimal pnl, decimal? recovery = null, decimal? maxDD = null, int? tradeCount = null)
	{
		var strategy = new Strategy();
		var stats = strategy.StatisticManager;
		var marketTime = DateTime.UtcNow;

		// Set PnL
		var pnlParam = stats.Parameters.OfType<NetProfitParameter>().First();
		pnlParam.Add(marketTime, pnl, null);

		// Set MaxDrawdown if provided - simulates equity curve to create drawdown
		if (maxDD.HasValue)
		{
			var maxDDParam = stats.Parameters.OfType<MaxDrawdownParameter>().First();
			// Simulate peak then drawdown
			maxDDParam.Add(marketTime, maxDD.Value, null);  // peak
			maxDDParam.Add(marketTime, 0m, null);           // drop to create maxDD
		}

		// Set Recovery if provided - depends on net profit and max drawdown being set
		if (recovery.HasValue)
		{
			var recoveryParam = stats.Parameters.OfType<RecoveryFactorParameter>().First();
			recoveryParam.Add(marketTime, pnl, null);
		}

		// Set TradeCount if provided
		if (tradeCount.HasValue)
		{
			var tradeCountParam = stats.Parameters.OfType<TradeCountParameter>().First();
			for (var i = 0; i < tradeCount.Value; i++)
				tradeCountParam.Add(new PnLInfo(marketTime, 1, 10m));
		}

		return strategy;
	}

	[TestMethod]
	public void Compile_SimpleFormula_ReturnsFunction()
	{
		var provider = CreateProvider();

		var fitness = provider.Compile("PnL");

		IsNotNull(fitness);
	}

	[TestMethod]
	public void Compile_SimpleFormula_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL");
		var strategy = CreateStrategyWithStats(pnl: 1000m);

		var result = fitness(strategy);

		result.AssertEqual(1000m);
	}

	[TestMethod]
	public void Compile_FormulaWithMultiplication_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL * 2");
		var strategy = CreateStrategyWithStats(pnl: 1000m);

		var result = fitness(strategy);

		result.AssertEqual(2000m);
	}

	[TestMethod]
	public void Compile_FormulaWithDivision_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL / TCount");
		var strategy = CreateStrategyWithStats(pnl: 1000m, tradeCount: 10);

		var result = fitness(strategy);

		result.AssertEqual(100m);
	}

	[TestMethod]
	public void Compile_FormulaWithSubtraction_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL - MaxDD");
		var strategy = CreateStrategyWithStats(pnl: 1000m, maxDD: 200m);

		var result = fitness(strategy);

		result.AssertEqual(800m);
	}

	[TestMethod]
	public void Compile_EmptyFormula_ThrowsArgumentNullException()
	{
		var provider = CreateProvider();

		Throws<ArgumentNullException>(() => provider.Compile(""));
	}

	[TestMethod]
	public void Compile_NullFormula_ThrowsArgumentNullException()
	{
		var provider = CreateProvider();

		Throws<ArgumentNullException>(() => provider.Compile(null));
	}

	[TestMethod]
	public void Compile_UnknownVariable_ThrowsArgumentOutOfRangeException()
	{
		var provider = CreateProvider();

		Throws<ArgumentOutOfRangeException>(() => provider.Compile("UnknownVar"));
	}

	[TestMethod]
	public void Compile_InvalidSyntax_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		Throws<InvalidOperationException>(() => provider.Compile("PnL +* Recovery"));
	}

	[TestMethod]
	public void Compile_UnbalancedParentheses_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		Throws<InvalidOperationException>(() => provider.Compile("(PnL + Recovery"));
	}

	[TestMethod]
	public void Compile_DivisionByZero_ThrowsAtEvaluation()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL / TCount");
		var strategy = CreateStrategyWithStats(pnl: 1000m, tradeCount: 0);

		// Division by zero should throw at evaluation time
		Throws<DivideByZeroException>(() => fitness(strategy));
	}

	[TestMethod]
	public void Compile_NegativeValues_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL");
		var strategy = CreateStrategyWithStats(pnl: -500m);

		var result = fitness(strategy);

		result.AssertEqual(-500m);
	}

	[TestMethod]
	public void Compile_ComplexExpression_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("(PnL + MaxDD) * 2");
		var strategy = CreateStrategyWithStats(pnl: 100m, maxDD: 50m);

		var result = fitness(strategy);

		result.AssertEqual(300m);
	}

	[TestMethod]
	public void Compile_SameFormulaTwice_ReturnsSeparateFunctions()
	{
		var provider = CreateProvider();

		var fitness1 = provider.Compile("PnL");
		var fitness2 = provider.Compile("PnL");

		// Should be different function instances
		fitness1.AssertNotSame(fitness2);
	}

	// ========== Real-world fitness formulas ==========

	[TestMethod]
	public void Compile_RiskAdjustedReturn_EvaluatesCorrectly()
	{
		// PnL / MaxDD - common risk-adjusted metric
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL / MaxDD");
		var strategy = CreateStrategyWithStats(pnl: 1000m, maxDD: 200m);

		var result = fitness(strategy);

		result.AssertEqual(5m);
	}

	[TestMethod]
	public void Compile_ProfitFactor_EvaluatesCorrectly()
	{
		// WinTrades / LosTrades - profit factor approximation
		var provider = CreateProvider();
		var fitness = provider.Compile("WinTrades / LosTrades");
		var strategy = CreateStrategyWithAllStats(winTrades: 60, losTrades: 40);

		var result = fitness(strategy);

		result.AssertEqual(1.5m);
	}

	[TestMethod]
	public void Compile_WeightedPnLByTrades_EvaluatesCorrectly()
	{
		// PnL * TCount / 100 - weighted by trade activity
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL * TCount / 100");
		var strategy = CreateStrategyWithStats(pnl: 500m, tradeCount: 20);

		var result = fitness(strategy);

		result.AssertEqual(100m);
	}

	[TestMethod]
	public void Compile_PnLWithPenalty_EvaluatesCorrectly()
	{
		// PnL * 2 - MaxDD - combines pnl with drawdown penalty
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL * 2 - MaxDD");
		var strategy = CreateStrategyWithStats(pnl: 1000m, maxDD: 500m);

		var result = fitness(strategy);

		result.AssertEqual(1500m); // 2 * 1000 - 500
	}

	// ========== Recovery variable tests ==========

	[TestMethod]
	public void Compile_RecoveryTimePnL_EvaluatesCorrectly()
	{
		// Recovery * PnL - tests Recovery variable
		var provider = CreateProvider();
		var fitness = provider.Compile("Recovery * PnL");
		var strategy = CreateStrategyWithAllStats(pnl: 1000m, recovery: 2m, maxDD: 500m);

		var result = fitness(strategy);

		result.AssertEqual(2000m); // 2 * 1000
	}

	[TestMethod]
	public void Compile_RecoveryWithPenalty_EvaluatesCorrectly()
	{
		// Recovery * PnL - MaxDD - combines recovery with drawdown penalty
		var provider = CreateProvider();
		var fitness = provider.Compile("Recovery * PnL - MaxDD");
		var strategy = CreateStrategyWithAllStats(pnl: 1000m, recovery: 2m, maxDD: 500m);

		var result = fitness(strategy);

		result.AssertEqual(1500m); // 2 * 1000 - 500
	}

	[TestMethod]
	public void Compile_RecoveryOnly_EvaluatesCorrectly()
	{
		// Just Recovery variable
		var provider = CreateProvider();
		var fitness = provider.Compile("Recovery");
		var strategy = CreateStrategyWithAllStats(pnl: 800m, maxDD: 400m, recovery: 2m);

		var result = fitness(strategy);

		result.AssertEqual(2m); // Recovery = PnL / MaxDD = 800 / 400 = 2
	}

	[TestMethod]
	public void Compile_RecoveryInComplexFormula_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("(PnL + Recovery) * 2");
		var strategy = CreateStrategyWithAllStats(pnl: 100m, recovery: 50m, maxDD: 200m);

		var result = fitness(strategy);

		result.AssertEqual(300m); // (100 + 50) * 2
	}

	// ========== Complex nested expressions ==========

	[TestMethod]
	public void Compile_NestedParentheses_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("((PnL + MaxDD) * (TCount - 5)) / 10");
		var strategy = CreateStrategyWithStats(pnl: 100m, maxDD: 50m, tradeCount: 15);

		var result = fitness(strategy);

		result.AssertEqual(150m); // ((100 + 50) * (15 - 5)) / 10 = 150 * 10 / 10 = 150
	}

	[TestMethod]
	public void Compile_DeeplyNestedParentheses_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("(((PnL)))");
		var strategy = CreateStrategyWithStats(pnl: 777m);

		var result = fitness(strategy);

		result.AssertEqual(777m);
	}

	[TestMethod]
	public void Compile_ComplexMultiVariable_EvaluatesCorrectly()
	{
		// (PnL * 3) / (MaxDD + 1)
		var provider = CreateProvider();
		var fitness = provider.Compile("(PnL * 3) / (MaxDD + 1)");
		var strategy = CreateStrategyWithStats(pnl: 500m, maxDD: 499m);

		var result = fitness(strategy);

		result.AssertEqual(3m); // (500 * 3) / (499 + 1) = 1500 / 500 = 3
	}

	// ========== Order of operations ==========

	[TestMethod]
	public void Compile_OrderOfOperations_MultiplicationBeforeAddition()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL + MaxDD * TCount");
		var strategy = CreateStrategyWithStats(pnl: 100m, maxDD: 10m, tradeCount: 5);

		var result = fitness(strategy);

		result.AssertEqual(150m); // 100 + (10 * 5) = 150, not (100 + 10) * 5 = 550
	}

	[TestMethod]
	public void Compile_OrderOfOperations_DivisionBeforeSubtraction()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL - MaxDD / TCount");
		var strategy = CreateStrategyWithStats(pnl: 100m, maxDD: 50m, tradeCount: 10);

		var result = fitness(strategy);

		result.AssertEqual(95m); // 100 - (50 / 10) = 100 - 5 = 95
	}

	[TestMethod]
	public void Compile_OrderOfOperations_ParenthesesOverride()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("(PnL + MaxDD) * TCount");
		var strategy = CreateStrategyWithStats(pnl: 100m, maxDD: 10m, tradeCount: 5);

		var result = fitness(strategy);

		result.AssertEqual(550m); // (100 + 10) * 5 = 550
	}

	// ========== Edge cases with numbers ==========

	[TestMethod]
	public void Compile_VeryLargeNumbers_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL * 1000");
		var strategy = CreateStrategyWithStats(pnl: 1_000_000_000m);

		var result = fitness(strategy);

		result.AssertEqual(1_000_000_000_000m);
	}

	[TestMethod]
	public void Compile_VerySmallNumbers_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL / TCount");
		var strategy = CreateStrategyWithStats(pnl: 0.001m, tradeCount: 1000);

		var result = fitness(strategy);

		result.AssertEqual(0.000001m);
	}

	[TestMethod]
	public void Compile_ZeroValue_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL * MaxDD");
		var strategy = CreateStrategyWithStats(pnl: 0m, maxDD: 100m);

		var result = fitness(strategy);

		result.AssertEqual(0m);
	}

	[TestMethod]
	public void Compile_AllNegativeValues_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL + TCount");
		var strategy = CreateStrategyWithStats(pnl: -100m, tradeCount: 0);

		var result = fitness(strategy);

		result.AssertEqual(-100m);
	}

	[TestMethod]
	public void Compile_MixedPositiveNegative_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL - MaxDD");
		var strategy = CreateStrategyWithStats(pnl: -100m, maxDD: 50m);

		var result = fitness(strategy);

		result.AssertEqual(-150m); // -100 - 50 = -150
	}

	// ========== Formulas with literals/constants ==========

	[TestMethod]
	public void Compile_FormulaWithConstant_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL * 2");
		var strategy = CreateStrategyWithStats(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(200m);
	}

	[TestMethod]
	public void Compile_FormulaWithDecimalConstant_EvaluatesCorrectly()
	{
		// Decimal literals are auto-converted (0.5 → 0.5m) by ExpressionHelper
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL * 0.5");
		var strategy = CreateStrategyWithStats(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(50m);
	}

	[TestMethod]
	public void Compile_FormulaWithNegativeConstant_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL + (-100)");
		var strategy = CreateStrategyWithStats(pnl: 250m);

		var result = fitness(strategy);

		result.AssertEqual(150m);
	}

	[TestMethod]
	public void Compile_ComplexWithMultipleConstants_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("(PnL - 50) * 2 + 100");
		var strategy = CreateStrategyWithStats(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(200m); // (100 - 50) * 2 + 100 = 50 * 2 + 100 = 200
	}

	// ========== All variables combinations ==========

	[TestMethod]
	public void Compile_AllFourArithmeticOperators_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL + 50 - MaxDD * TCount / 10");
		var strategy = CreateStrategyWithStats(pnl: 100m, maxDD: 20m, tradeCount: 10);

		var result = fitness(strategy);

		// 100 + 50 - (20 * 10 / 10) = 100 + 50 - 20 = 130
		result.AssertEqual(130m);
	}

	[TestMethod]
	public void Compile_MultipleVariablesSameType_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("WinTrades - LosTrades");
		var strategy = CreateStrategyWithAllStats(winTrades: 70, losTrades: 30);

		var result = fitness(strategy);

		result.AssertEqual(40m);
	}

	// ========== Error cases - more variations ==========

	[TestMethod]
	public void Compile_EmptyParentheses_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		Throws<InvalidOperationException>(() => provider.Compile("PnL + ()"));
	}

	[TestMethod]
	public void Compile_MissingOperator_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		Throws<InvalidOperationException>(() => provider.Compile("PnL Recovery"));
	}

	[TestMethod]
	public void Compile_TrailingOperator_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		Throws<InvalidOperationException>(() => provider.Compile("PnL +"));
	}

	[TestMethod]
	public void Compile_LeadingOperator_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		Throws<InvalidOperationException>(() => provider.Compile("* PnL"));
	}

	[TestMethod]
	public void Compile_DoubleOperator_ThrowsInvalidOperationException()
	{
		var provider = CreateProvider();

		Throws<InvalidOperationException>(() => provider.Compile("PnL ++ Recovery"));
	}

	[TestMethod]
	public void Compile_WhitespaceOnly()
	{
		var provider = CreateProvider();

		Throws<ArgumentException>(() => provider.Compile("   "));
	}

	[TestMethod]
	public void Compile_TabsOnly()
	{
		var provider = CreateProvider();

		Throws<ArgumentException>(() => provider.Compile("\t\t"));
	}

	[TestMethod]
	public void Compile_NewlinesOnly()
	{
		var provider = CreateProvider();

		Throws<ArgumentException>(() => provider.Compile("\n\n"));
	}

	[TestMethod]
	public void Compile_CaseInsensitiveVariable_EvaluatesCorrectly()
	{
		// Variables are case-insensitive
		var provider = CreateProvider();
		var fitness = provider.Compile("pnl");
		var strategy = CreateStrategyWithStats(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(100m);
	}

	[TestMethod]
	public void Compile_MixedCaseVariable_EvaluatesCorrectly()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PNL");
		var strategy = CreateStrategyWithStats(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(100m);
	}

	// ========== More decimal literal edge cases ==========

	[TestMethod]
	public void Compile_DivisionByDecimal_EvaluatesCorrectly()
	{
		// Decimal literals are auto-converted (0.5 → 0.5m) by ExpressionHelper
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL / 0.5");
		var strategy = CreateStrategyWithStats(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(200m); // 100 / 0.5 = 200
	}

	[TestMethod]
	public void Compile_AdditionWithDecimal_EvaluatesCorrectly()
	{
		// Decimal literals are auto-converted (0.1 → 0.1m) by ExpressionHelper
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL + 0.1");
		var strategy = CreateStrategyWithStats(pnl: 100m);

		var result = fitness(strategy);

		result.AssertEqual(100.1m);
	}

	// ========== Order of operations with Recovery ==========

	[TestMethod]
	public void Compile_OrderOfOperations_RecoveryMultiplicationBeforeAddition()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("PnL + Recovery * TCount");
		var strategy = CreateStrategyWithAllStats(pnl: 100m, recovery: 10m, maxDD: 100m, tradeCount: 5);

		var result = fitness(strategy);

		result.AssertEqual(150m); // 100 + (10 * 5) = 150
	}

	[TestMethod]
	public void Compile_OrderOfOperations_RecoveryParenthesesOverride()
	{
		var provider = CreateProvider();
		var fitness = provider.Compile("(PnL + Recovery) * TCount");
		var strategy = CreateStrategyWithAllStats(pnl: 100m, recovery: 10m, maxDD: 100m, tradeCount: 5);

		var result = fitness(strategy);

		result.AssertEqual(550m); // (100 + 10) * 5 = 550
	}

	// ========== Helper for full stats ==========

	private static Strategy CreateStrategyWithAllStats(
		decimal? pnl = null,
		decimal? recovery = null,
		decimal? maxDD = null,
		int? tradeCount = null,
		int? winTrades = null,
		int? losTrades = null)
	{
		var strategy = new Strategy();
		var stats = strategy.StatisticManager;
		var marketTime = DateTime.UtcNow;

		if (pnl.HasValue)
		{
			var param = stats.Parameters.OfType<NetProfitParameter>().First();
			param.Add(marketTime, pnl.Value, null);
		}

		if (maxDD.HasValue)
		{
			var param = stats.Parameters.OfType<MaxDrawdownParameter>().First();
			param.Add(marketTime, maxDD.Value, null);
			param.Add(marketTime, 0m, null);
		}

		if (recovery.HasValue)
		{
			var param = stats.Parameters.OfType<RecoveryFactorParameter>().First();
			param.Add(marketTime, pnl ?? 0m, null);
		}

		if (tradeCount.HasValue)
		{
			var param = stats.Parameters.OfType<TradeCountParameter>().First();
			for (var i = 0; i < tradeCount.Value; i++)
				param.Add(new PnLInfo(marketTime, 1, 10m));
		}

		if (winTrades.HasValue)
		{
			var param = stats.Parameters.OfType<WinningTradesParameter>().First();
			for (var i = 0; i < winTrades.Value; i++)
				param.Add(new PnLInfo(marketTime, 1, 100m));
		}

		if (losTrades.HasValue)
		{
			var param = stats.Parameters.OfType<LossingTradesParameter>().First();
			for (var i = 0; i < losTrades.Value; i++)
				param.Add(new PnLInfo(marketTime, 1, -50m));
		}

		return strategy;
	}
}
