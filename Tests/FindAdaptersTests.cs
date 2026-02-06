namespace StockSharp.Tests;

#region Test Adapter Types (top-level for FindImplementations visibility)

/// <summary>
/// Valid adapter with public constructor accepting IdGenerator.
/// </summary>
public class FindAdaptersTestValidAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
{
	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
}

/// <summary>
/// Invalid adapter without IdGenerator constructor.
/// </summary>
public class FindAdaptersTestNoIdGeneratorConstructorAdapter() : MessageAdapter(new IncrementalIdGenerator())
{
	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
}

/// <summary>
/// Invalid adapter with private constructor only.
/// </summary>
public class FindAdaptersTestPrivateConstructorAdapter : MessageAdapter
{
	private FindAdaptersTestPrivateConstructorAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
	}

	public static FindAdaptersTestPrivateConstructorAdapter Create(IdGenerator gen) => new(gen);

	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
}

/// <summary>
/// Dialect adapter - should be filtered out by name.
/// </summary>
public class FindAdaptersTestDialect(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
{
	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
}

/// <summary>
/// Abstract adapter - should be filtered out.
/// </summary>
public abstract class FindAdaptersTestAbstractAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
{
}

/// <summary>
/// Adapter with multiple constructors - one valid.
/// </summary>
public class FindAdaptersTestMultipleConstructorsAdapter : MessageAdapter
{
	public FindAdaptersTestMultipleConstructorsAdapter()
		: base(new IncrementalIdGenerator())
	{
	}

	public FindAdaptersTestMultipleConstructorsAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
	}

	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
}

#endregion

/// <summary>
/// Tests for <see cref="Extensions.FindAdapters"/> method.
/// </summary>
[TestClass]
public class FindAdaptersTests
{
	[TestMethod]
	public void HasValidAdapterConstructor_ValidAdapter_ReturnsTrue()
	{
		typeof(FindAdaptersTestValidAdapter).HasValidAdapterConstructor().AssertTrue();
	}

	[TestMethod]
	public void HasValidAdapterConstructor_NoIdGeneratorConstructor_ReturnsFalse()
	{
		typeof(FindAdaptersTestNoIdGeneratorConstructorAdapter).HasValidAdapterConstructor().AssertFalse();
	}

	[TestMethod]
	public void HasValidAdapterConstructor_PrivateConstructor_ReturnsFalse()
	{
		typeof(FindAdaptersTestPrivateConstructorAdapter).HasValidAdapterConstructor().AssertFalse();
	}

	[TestMethod]
	public void HasValidAdapterConstructor_MultipleConstructors_ReturnsTrue()
	{
		typeof(FindAdaptersTestMultipleConstructorsAdapter).HasValidAdapterConstructor().AssertTrue();
	}

	[TestMethod]
	public void FindAdapters_FiltersDialects()
	{
		var type = typeof(FindAdaptersTestDialect);

		// Type has valid constructor
		type.HasValidAdapterConstructor().AssertTrue();

		// But name ends with "Dialect" - should be filtered
		type.Name.EndsWith("Dialect").AssertTrue();
	}

	[TestMethod]
	public void GetAdapters_FromAssembly_FindsValidAdapters()
	{
		var asm = typeof(FindAdaptersTestValidAdapter).Assembly;
		var adapters = asm.GetAdapters().ToArray();

		// Valid adapters should be found
		adapters.Count(a => a == typeof(FindAdaptersTestValidAdapter)).AssertEqual(1, "ValidAdapter should be found");
		adapters.Count(a => a == typeof(FindAdaptersTestMultipleConstructorsAdapter)).AssertEqual(1, "MultipleConstructorsAdapter should be found");
	}

	[TestMethod]
	public void GetAdapters_FromAssembly_FiltersOutDialect()
	{
		var asm = typeof(FindAdaptersTestDialect).Assembly;
		var adapters = asm.GetAdapters().ToArray();

		adapters.Count(a => a == typeof(FindAdaptersTestDialect)).AssertEqual(0, "Dialect adapter should be filtered out");
	}

	[TestMethod]
	public void GetAdapters_FromAssembly_FiltersOutAbstract()
	{
		var asm = typeof(FindAdaptersTestAbstractAdapter).Assembly;
		var adapters = asm.GetAdapters().ToArray();

		adapters.Count(a => a == typeof(FindAdaptersTestAbstractAdapter)).AssertEqual(0, "Abstract adapter should be filtered out");
	}

	[TestMethod]
	public void GetAdapters_FromAssembly_FiltersOutInvalidConstructors()
	{
		var asm = typeof(FindAdaptersTestNoIdGeneratorConstructorAdapter).Assembly;
		var adapters = asm.GetAdapters().ToArray();

		adapters.Count(a => a == typeof(FindAdaptersTestNoIdGeneratorConstructorAdapter)).AssertEqual(0, "No-IdGenerator adapter should be filtered out");
		adapters.Count(a => a == typeof(FindAdaptersTestPrivateConstructorAdapter)).AssertEqual(0, "Private constructor adapter should be filtered out");
	}

	[TestMethod]
	public void CreateAdapter_ValidType_CreatesInstance()
	{
		var type = typeof(FindAdaptersTestValidAdapter);
		var idGenerator = new IncrementalIdGenerator();

		var adapter = type.CreateAdapter(idGenerator);

		adapter.AssertNotNull();
		adapter.AssertOfType<FindAdaptersTestValidAdapter>();
	}

	[TestMethod]
	public void CreateAdapter_NoIdGeneratorConstructor_ThrowsException()
	{
		var type = typeof(FindAdaptersTestNoIdGeneratorConstructorAdapter);
		var idGenerator = new IncrementalIdGenerator();

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
		var type = typeof(FindAdaptersTestPrivateConstructorAdapter);
		var idGenerator = new IncrementalIdGenerator();

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
		Type type = null;

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
