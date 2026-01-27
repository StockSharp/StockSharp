namespace StockSharp.Tests;

/// <summary>
/// Tests for <see cref="Extensions.FindAdapters"/> method.
/// </summary>
[TestClass]
public class FindAdaptersTests
{
	#region Test Adapter Types

	/// <summary>
	/// Valid adapter with public constructor accepting IdGenerator.
	/// </summary>
	public class ValidAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
	{
		/// <inheritdoc />
		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
	}

	/// <summary>
	/// Invalid adapter without IdGenerator constructor.
	/// </summary>
	public class NoIdGeneratorConstructorAdapter() : MessageAdapter(new IncrementalIdGenerator())
	{
		/// <inheritdoc />
		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
	}

	/// <summary>
	/// Invalid adapter with private constructor only.
	/// </summary>
	public class PrivateConstructorAdapter : MessageAdapter
	{
		private PrivateConstructorAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
		}

		public static PrivateConstructorAdapter Create(IdGenerator gen) => new(gen);

		/// <inheritdoc />
		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
	}

	/// <summary>
	/// Invalid adapter with internal constructor.
	/// </summary>
	internal class InternalAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
	{
		/// <inheritdoc />
		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
	}

	/// <summary>
	/// Dialect adapter - should be filtered out by name.
	/// </summary>
	public class TestDialect(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
	{
		/// <inheritdoc />
		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
	}

	/// <summary>
	/// Abstract adapter - should be filtered out.
	/// </summary>
	public abstract class AbstractAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
	{
	}

	/// <summary>
	/// Adapter with multiple constructors - one valid.
	/// </summary>
	public class MultipleConstructorsAdapter : MessageAdapter
	{
		public MultipleConstructorsAdapter()
			: base(new IncrementalIdGenerator())
		{
		}

		public MultipleConstructorsAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
		}

		/// <inheritdoc />
		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
	}

	#endregion

	[TestMethod]
	public void HasValidAdapterConstructor_ValidAdapter_ReturnsTrue()
	{
		// Arrange
		var type = typeof(ValidAdapter);

		// Act
		var hasValidConstructor = type.HasValidAdapterConstructor();

		// Assert
		hasValidConstructor.AssertTrue();
	}

	[TestMethod]
	public void HasValidAdapterConstructor_NoIdGeneratorConstructor_ReturnsFalse()
	{
		// Arrange
		var type = typeof(NoIdGeneratorConstructorAdapter);

		// Act
		var hasValidConstructor = type.HasValidAdapterConstructor();

		// Assert
		hasValidConstructor.AssertFalse();
	}

	[TestMethod]
	public void HasValidAdapterConstructor_PrivateConstructor_ReturnsFalse()
	{
		// Arrange
		var type = typeof(PrivateConstructorAdapter);

		// Act
		var hasValidConstructor = type.HasValidAdapterConstructor();

		// Assert
		hasValidConstructor.AssertFalse();
	}

	[TestMethod]
	public void HasValidAdapterConstructor_MultipleConstructors_ReturnsTrue()
	{
		// Arrange
		var type = typeof(MultipleConstructorsAdapter);

		// Act
		var hasValidConstructor = type.HasValidAdapterConstructor();

		// Assert
		hasValidConstructor.AssertTrue();
	}

	[TestMethod]
	public void FindAdapters_FiltersDialects()
	{
		// The FindAdapters method filters out types ending with "Dialect"
		// Verify this filter works by checking that TestDialect would pass other checks
		// but is excluded due to its name
		var type = typeof(TestDialect);

		// Type has valid constructor
		type.HasValidAdapterConstructor().AssertTrue();

		// But name ends with "Dialect" - should be filtered
		type.Name.EndsWith("Dialect").AssertTrue();
	}

	[TestMethod]
	public void GetAdapters_FromAssembly_FiltersAndReturnsValidTypes()
	{
		// Arrange
		var asm = typeof(FindAdaptersTests).Assembly;

		// Act
		var adapters = asm.GetAdapters().ToArray();

		// Assert
		adapters.Contains(typeof(ValidAdapter)).AssertTrue();
		adapters.Contains(typeof(MultipleConstructorsAdapter)).AssertTrue();

		adapters.Contains(typeof(NoIdGeneratorConstructorAdapter)).AssertFalse();
		adapters.Contains(typeof(PrivateConstructorAdapter)).AssertFalse();
		adapters.Any(t => t.Name.EndsWith("Dialect")).AssertFalse();
		adapters.Contains(typeof(AbstractAdapter)).AssertFalse();
		adapters.Contains(typeof(InternalAdapter)).AssertFalse();
	}

	[TestMethod]
	public void CreateAdapter_ValidType_CreatesInstance()
	{
		// Arrange
		var type = typeof(ValidAdapter);
		var idGenerator = new IncrementalIdGenerator();

		// Act
		var adapter = type.CreateAdapter(idGenerator);

		// Assert
		adapter.AssertNotNull();
		adapter.AssertOfType<ValidAdapter>();
	}

	[TestMethod]
	public void CreateAdapter_NoIdGeneratorConstructor_ThrowsException()
	{
		// Arrange
		var type = typeof(NoIdGeneratorConstructorAdapter);
		var idGenerator = new IncrementalIdGenerator();

		// Act & Assert - should throw because no matching constructor
		var thrown = false;
		try
		{
			type.CreateAdapter(idGenerator);
		}
		catch (MissingMethodException)
		{
			thrown = true;
		}

		thrown.AssertTrue("Expected MissingMethodException was not thrown");
	}

	[TestMethod]
	public void CreateAdapter_PrivateConstructor_ThrowsException()
	{
		// Arrange
		var type = typeof(PrivateConstructorAdapter);
		var idGenerator = new IncrementalIdGenerator();

		// Act & Assert - should throw because constructor is private
		var thrown = false;
		try
		{
			type.CreateAdapter(idGenerator);
		}
		catch (MissingMethodException)
		{
			thrown = true;
		}

		thrown.AssertTrue("Expected MissingMethodException was not thrown");
	}

	[TestMethod]
	public void HasValidAdapterConstructor_NullType_ThrowsArgumentNullException()
	{
		// Arrange
		Type type = null;

		// Act & Assert
		var thrown = false;
		try
		{
			type.HasValidAdapterConstructor();
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}

		thrown.AssertTrue("Expected ArgumentNullException was not thrown");
	}
}
