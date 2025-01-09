namespace StockSharp.Algo.Compilation;

using Ecng.Compilation;

/// <summary>
/// Extension class.
/// </summary>
public static class CodeExtensions
{
	/// <summary>
	/// Microsoft Core Library.
	/// </summary>
	public const string MsCorLib = "mscorlib";

	private static readonly IEnumerable<string> _defaultReferences =
	[
		MsCorLib,

		"netstandard",

		"System",
		"System.Core",
		"System.Configuration",
		"System.Xml",
		"System.Runtime",
		"System.Linq",
		"System.Private.CoreLib",
		"System.ObjectModel",
		"System.ComponentModel.Primitives",
		"System.ComponentModel.TypeConverter",
		"System.ComponentModel.Annotations",
		"System.Collections",
		"System.Drawing.Primitives",

		"Ecng.Common",
		"Ecng.Collections",
		"Ecng.ComponentModel",
		"Ecng.Configuration",
		"Ecng.Serialization",
		"Ecng.Reflection",
		"Ecng.Drawing",

		"StockSharp.Algo",
		"StockSharp.Messages",
		"StockSharp.BusinessEntities",
		"StockSharp.Logging",
		"StockSharp.Localization",
		"StockSharp.Diagram.Core",
		"StockSharp.Charting.Interfaces",
		"StockSharp.Alerts.Interfaces",
	];

	/// <summary>
	/// Default references.
	/// </summary>
	public static IEnumerable<AssemblyReference> DefaultReferences
		=>	_defaultReferences
			.Select(r => new AssemblyReference { FileName = $"{r}.dll" })
			.ToArray();

	private static readonly CachedSynchronizedDictionary<string, ICodeReference> _projectReferences = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Project references.
	/// </summary>
	public static IEnumerable<ICodeReference> ProjectReferences => _projectReferences.CachedValues;

	/// <summary>
	/// Add project reference.
	/// </summary>
	/// <param name="reference"><see cref="ICodeReference"/></param>
	public static void AddProjectReference(ICodeReference reference)
	{
		if (reference is null)
			throw new ArgumentNullException(nameof(reference));

		_projectReferences.Add(reference.Id, reference);
	}

	/// <summary>
	/// Try get project reference.
	/// </summary>
	/// <param name="id"><see cref="ICodeReference.Id"/></param>
	/// <param name="reference"><see cref="ICodeReference"/></param>
	/// <returns>Check result.</returns>
	public static bool TryGetProjectReference(string id, out ICodeReference reference)
	{
		if (id.IsEmpty())
			throw new ArgumentNullException(nameof(id));

		return _projectReferences.TryGetValue(id, out reference);
	}

	/// <summary>
	/// Remove project reference.
	/// </summary>
	/// <param name="id"><see cref="ICodeReference.Id"/></param>
	/// <returns>Check result.</returns>
	public static bool RemoveProjectReference(string id)
		=> _projectReferences.Remove(id);

	/// <summary>
	/// Add assembly reference.
	/// </summary>
	/// <param name="ci"><see cref="CodeInfo"/></param>
	/// <param name="asmFile">Assembly path.</param>
	public static void AddAsmRef(this CodeInfo ci, string asmFile)
		=> ci.CheckOnNull(nameof(ci)).AssemblyReferences.Add(new() { FileName = asmFile });

	/// <summary>
	/// Get C# compiler.
	/// </summary>
	/// <returns><see cref="ICompiler"/></returns>
	public static ICompiler GetCSharpCompiler()
		=> TryGetCSharpCompiler() ?? throw new InvalidOperationException($"No compiler for {CompilationLanguages.CSharp}.");

	/// <summary>
	/// Try get C# compiler.
	/// </summary>
	/// <returns><see cref="ICompiler"/></returns>
	public static ICompiler TryGetCSharpCompiler()
		=> CompilationLanguages.CSharp.TryGetCompiler();

	/// <summary>
	/// Try get compiler for the specified language.
	/// </summary>
	/// <param name="language"><see cref="CompilationLanguages"/></param>
	/// <returns><see cref="ICompiler"/></returns>
	public static ICompiler GetCompiler(this CompilationLanguages language)
		=> TryGetCompiler(language) ?? throw new InvalidOperationException($"No compiler for {language}.");

	/// <summary>
	/// Try get compiler for the specified language.
	/// </summary>
	/// <param name="language"><see cref="CompilationLanguages"/></param>
	/// <returns><see cref="ICompiler"/></returns>
	public static ICompiler TryGetCompiler(this CompilationLanguages language)
	{
		var provider = ServicesRegistry.TryCompilerProvider;

		ICompiler compiler;

		if (provider is not null)
		{
			if (!provider.TryGetValue(language, out compiler))
				return null;
		}
		else
		{
			compiler = ServicesRegistry.TryCompiler;

			if (compiler?.Language != language)
				return null;
		}

		return compiler;
	}
}