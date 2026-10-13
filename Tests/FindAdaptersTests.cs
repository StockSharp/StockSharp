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
public class FindAdaptersTests : BaseTestClass
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

	#region Directory scan

	private static string CreateScanDir()
		=> Path.Combine(Path.GetTempPath(), "ss_findadapters_" + Guid.NewGuid().ToString("N"));

	// This assembly is a real one and holds the adapter types above, so copying its file into a scan
	// directory under a chosen name is how a test decides what the scan is looking at.
	private static void PlaceRealAssembly(string dir, string fileName)
	{
		Directory.CreateDirectory(dir);
		File.Copy(typeof(FindAdaptersTestValidAdapter).Assembly.Location, Path.Combine(dir, fileName));
	}

	private static void PlaceJunk(string dir, string fileName)
	{
		Directory.CreateDirectory(dir);
		File.WriteAllText(Path.Combine(dir, fileName), "this is not an assembly");
	}

	private static void Delete(string dir)
	{
		if (Directory.Exists(dir))
			Directory.Delete(dir, true);
	}

	/// <summary>
	/// Dropping a connector's assembly into the program's folder is how a connector is installed, so
	/// the scan of that folder has to come back with the adapters that assembly holds.
	/// </summary>
	[TestMethod]
	public void FindAdapters_FindsTheAdaptersInTheAssembliesItScans()
	{
		var dir = CreateScanDir();

		try
		{
			PlaceRealAssembly(dir, "StockSharp.Scan.dll");

			var errors = new List<Exception>();
			var adapters = dir.FindAdapters(errors.Add).ToArray();

			Contains(adapters, typeof(FindAdaptersTestValidAdapter), "an adapter sitting in the scanned folder is found");
			IsEmpty(errors, "reading a good assembly is not an error");
		}
		finally
		{
			Delete(dir);
		}
	}

	/// <summary>
	/// The scan only opens assemblies that are ours. Anything else in the folder belongs to somebody
	/// else and is left alone - loading it could run its module initializer for no reason at all.
	/// </summary>
	[TestMethod]
	public void FindAdapters_LeavesAssembliesThatAreNotOursAlone()
	{
		var dir = CreateScanDir();

		try
		{
			PlaceRealAssembly(dir, "SomebodyElse.Scan.dll");

			var errors = new List<Exception>();
			var adapters = dir.FindAdapters(errors.Add).ToArray();

			IsEmpty(adapters, "a file outside our own naming is not opened, whatever it holds");
			IsEmpty(errors);
		}
		finally
		{
			Delete(dir);
		}
	}

	/// <summary>
	/// One unreadable file in the folder - a half-written download, a stub - must not cost the user
	/// every other connector installed next to it. The scan goes on and still returns the rest.
	/// </summary>
	[TestMethod]
	public void FindAdapters_BrokenAssemblyNextToAGoodOne_DoesNotStopTheScan()
	{
		var dir = CreateScanDir();

		try
		{
			PlaceJunk(dir, "StockSharp.Broken.dll");
			PlaceRealAssembly(dir, "StockSharp.Scan.dll");

			var errors = new List<Exception>();
			var adapters = dir.FindAdapters(errors.Add).ToArray();

			Contains(adapters, typeof(FindAdaptersTestValidAdapter), "the assembly next to the broken one is still read");
		}
		finally
		{
			Delete(dir);
		}
	}

	/// <summary>
	/// A folder that is not there is a configuration mistake, not a reason to bring the program down
	/// on start-up: the scan hands the problem to the error handler and comes back empty-handed.
	/// </summary>
	[TestMethod]
	public void FindAdapters_DirectoryThatIsNotThere_ReachesTheErrorHandler()
	{
		var dir = CreateScanDir();

		var errors = new List<Exception>();
		var adapters = dir.FindAdapters(errors.Add).ToArray();

		IsEmpty(adapters, "nothing was found because there was nowhere to look");
		errors.Count.AssertEqual(1, "the caller is told why nothing was found");
	}

	#endregion
}
