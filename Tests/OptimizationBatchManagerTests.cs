namespace StockSharp.Tests;

using StockSharp.Algo.Strategies.Optimization;

[TestClass]
public class OptimizationBatchManagerTests : BaseTestClass
{
	[TestMethod]
	public void Reset_SetsInitialState()
	{
		var manager = new OptimizationBatchManager();

		manager.Reset(batchSize: 4, totalIterations: 100);

		AreEqual(4, manager.BatchSize);
		AreEqual(0, manager.RunningCount);
		IsTrue(manager.CanStartNext);
		IsFalse(manager.IsFinished);
	}

	[TestMethod]
	public void RegisterRunning_IncreasesRunningCount()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 4, totalIterations: 100);

		manager.RegisterRunning(Guid.NewGuid());
		manager.RegisterRunning(Guid.NewGuid());

		AreEqual(2, manager.RunningCount);
		AreEqual(2, manager.StartedCount);
	}

	[TestMethod]
	public void RegisterRunning_DuplicateThrows()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 4, totalIterations: 100);

		var id = Guid.NewGuid();
		manager.RegisterRunning(id);

		ThrowsExactly<InvalidOperationException>(() => manager.RegisterRunning(id));
	}

	[TestMethod]
	public void CanStartNext_FalseWhenBatchFull()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 2, totalIterations: 100);

		manager.RegisterRunning(Guid.NewGuid());
		manager.RegisterRunning(Guid.NewGuid());

		IsFalse(manager.CanStartNext);
	}

	[TestMethod]
	public void CanStartNext_FalseWhenAllStarted()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 4, totalIterations: 2);

		manager.RegisterRunning(Guid.NewGuid());
		manager.RegisterRunning(Guid.NewGuid());

		IsFalse(manager.CanStartNext);
	}

	[TestMethod]
	public void CompleteIteration_DecreasesRunningCount()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 4, totalIterations: 100);

		var id1 = Guid.NewGuid();
		var id2 = Guid.NewGuid();
		manager.RegisterRunning(id1);
		manager.RegisterRunning(id2);

		manager.CompleteIteration(id1);

		AreEqual(1, manager.RunningCount);
		AreEqual(2, manager.StartedCount); // started count doesn't decrease
	}

	[TestMethod]
	public void CompleteIteration_ReturnsTrueWhenShouldStartNext()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 2, totalIterations: 10);

		var id1 = Guid.NewGuid();
		var id2 = Guid.NewGuid();
		manager.RegisterRunning(id1);
		manager.RegisterRunning(id2);

		var shouldStartNext = manager.CompleteIteration(id1);

		IsTrue(shouldStartNext);
	}

	[TestMethod]
	public void CompleteIteration_ReturnsFalseWhenAllStarted()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 2, totalIterations: 2);

		var id1 = Guid.NewGuid();
		var id2 = Guid.NewGuid();
		manager.RegisterRunning(id1);
		manager.RegisterRunning(id2);

		var shouldStartNext = manager.CompleteIteration(id1);

		IsFalse(shouldStartNext);
	}

	[TestMethod]
	public void CompleteIteration_UnknownIdThrows()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 4, totalIterations: 100);

		ThrowsExactly<InvalidOperationException>(() => manager.CompleteIteration(Guid.NewGuid()));
	}

	[TestMethod]
	public void IsFinished_TrueWhenAllComplete()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 2, totalIterations: 2);

		var id1 = Guid.NewGuid();
		var id2 = Guid.NewGuid();
		manager.RegisterRunning(id1);
		manager.RegisterRunning(id2);
		manager.CompleteIteration(id1);
		manager.CompleteIteration(id2);

		IsTrue(manager.IsFinished);
	}

	[TestMethod]
	public void IsFinished_FalseWhileRunning()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 2, totalIterations: 2);

		var id1 = Guid.NewGuid();
		var id2 = Guid.NewGuid();
		manager.RegisterRunning(id1);
		manager.RegisterRunning(id2);
		manager.CompleteIteration(id1);

		IsFalse(manager.IsFinished);
	}

	[TestMethod]
	public void TryReserveSlot_ReturnsUniqueIds()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 4, totalIterations: 100);

		var ids = new HashSet<Guid>();

		for (var i = 0; i < 4; i++)
		{
			IsTrue(manager.TryReserveSlot(out var id));
			IsTrue(ids.Add(id)); // unique
		}
	}

	[TestMethod]
	public void TryReserveSlot_FailsWhenBatchFull()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 2, totalIterations: 100);

		manager.TryReserveSlot(out _);
		manager.TryReserveSlot(out _);

		IsFalse(manager.TryReserveSlot(out _));
	}

	[TestMethod]
	public void BatchTransition_Scenario()
	{
		// Test realistic scenario: batch of 2, total 5 iterations
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 2, totalIterations: 5);

		// Start batch 1: iterations 1, 2
		IsTrue(manager.TryReserveSlot(out var id1));
		IsTrue(manager.TryReserveSlot(out var id2));
		IsFalse(manager.TryReserveSlot(out _)); // batch full

		AreEqual(2, manager.RunningCount);
		AreEqual(2, manager.StartedCount);
		AreEqual(3, manager.RemainingToStart);

		// Complete iteration 1 -> should start iteration 3
		IsTrue(manager.CompleteIteration(id1));
		IsTrue(manager.TryReserveSlot(out var id3));

		AreEqual(2, manager.RunningCount);
		AreEqual(3, manager.StartedCount);

		// Complete iteration 2 -> should start iteration 4
		IsTrue(manager.CompleteIteration(id2));
		IsTrue(manager.TryReserveSlot(out var id4));

		// Complete iteration 3 -> should start iteration 5
		IsTrue(manager.CompleteIteration(id3));
		IsTrue(manager.TryReserveSlot(out var id5));

		AreEqual(5, manager.StartedCount);
		AreEqual(0, manager.RemainingToStart);

		// Complete iteration 4 -> no more to start
		IsFalse(manager.CompleteIteration(id4));
		IsFalse(manager.TryReserveSlot(out _));

		IsFalse(manager.IsFinished); // id5 still running

		// Complete iteration 5 -> finished
		manager.CompleteIteration(id5);
		IsTrue(manager.IsFinished);
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var manager = new OptimizationBatchManager();
		manager.Reset(batchSize: 2, totalIterations: 5);

		manager.TryReserveSlot(out var id1);
		manager.TryReserveSlot(out _);
		manager.CompleteIteration(id1);

		// Reset and verify clean state
		manager.Reset(batchSize: 3, totalIterations: 10);

		AreEqual(3, manager.BatchSize);
		AreEqual(0, manager.RunningCount);
		AreEqual(0, manager.StartedCount);
		AreEqual(10, manager.RemainingToStart);
		IsFalse(manager.IsFinished);
	}

	[TestMethod]
	public void Reset_ThrowsOnInvalidBatchSize()
	{
		var manager = new OptimizationBatchManager();

		ThrowsExactly<ArgumentOutOfRangeException>(() => manager.Reset(batchSize: 0, totalIterations: 10));
		ThrowsExactly<ArgumentOutOfRangeException>(() => manager.Reset(batchSize: -1, totalIterations: 10));
	}

	[TestMethod]
	public void Reset_ThrowsOnNegativeIterations()
	{
		var manager = new OptimizationBatchManager();

		ThrowsExactly<ArgumentOutOfRangeException>(() => manager.Reset(batchSize: 2, totalIterations: -1));
	}
}
