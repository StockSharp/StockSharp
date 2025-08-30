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

	/// <summary>
	/// Default references.
	/// </summary>
	public static IEnumerable<AssemblyReference> DefaultReferences
		=> CreateAssemblyReferences(
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
			"Ecng.Logging",

			"StockSharp.Algo",
			"StockSharp.Messages",
			"StockSharp.BusinessEntities",
			"StockSharp.Localization",
			"StockSharp.Diagram.Core",
			"StockSharp.Charting.Interfaces",
			"StockSharp.Alerts.Interfaces",
		]);

	/// <summary>
	/// F# references.
	/// </summary>
	public static IEnumerable<AssemblyReference> FSharpReferences
		=> CreateAssemblyReferences(
		[
			"FSharp.Core",
			"System.Runtime.Numerics",
			"System.Numerics",
			"System.Net.Requests",
			"System.Net.WebClient",
			"System.Private.Uri",
			"System.Threading",
		]);

	/// <summary>
	/// Create assembly references.
	/// </summary>
	/// <param name="names">Assembly names.</param>
	/// <returns>List of <see cref="AssemblyReference"/>.</returns>
	public static IEnumerable<AssemblyReference> CreateAssemblyReferences(this IEnumerable<string> names)
		=> names.Select(r => new AssemblyReference { FileName = $"{r}.dll" });

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
	/// Default language.
	/// </summary>
	public const string DefaultLanguage = FileExts.CSharp;

	/// <summary>
	/// Get C# compiler.
	/// </summary>
	/// <returns><see cref="ICompiler"/></returns>
	public static ICompiler GetCSharpCompiler()
		=> TryGetCSharpCompiler() ?? throw new InvalidOperationException($"No compiler for {DefaultLanguage}.");

	/// <summary>
	/// Try get C# compiler.
	/// </summary>
	/// <returns><see cref="ICompiler"/></returns>
	public static ICompiler TryGetCSharpCompiler()
		=> TryGetCompiler(DefaultLanguage);

	/// <summary>
	/// Try get compiler for the specified file extension.
	/// </summary>
	/// <param name="fileExt">File extension.</param>
	/// <returns><see cref="ICompiler"/></returns>
	public static ICompiler GetCompiler(this string fileExt)
		=> TryGetCompiler(fileExt) ?? throw new InvalidOperationException($"No compiler for {fileExt}.");

	/// <summary>
	/// Try get compiler for the specified language.
	/// </summary>
	/// <param name="fileExt">File extension.</param>
	/// <returns><see cref="ICompiler"/></returns>
	public static ICompiler TryGetCompiler(this string fileExt)
	{
		if (fileExt.IsEmpty())
			throw new ArgumentNullException(nameof(fileExt));

		var provider = ServicesRegistry.TryCompilerProvider;

		ICompiler compiler;

		if (provider is not null)
		{
			if (!provider.TryGetValue(fileExt, out compiler))
				return null;
		}
		else
		{
			compiler = ServicesRegistry.TryCompiler;

			if (compiler?.Extension.EqualsIgnoreCase(fileExt) != true)
				return null;
		}

		return compiler;
	}

	/// <summary>
	/// Determine whether the specified file extension is a code file.
	/// </summary>
	/// <param name="fileExt">File extension.</param>
	/// <returns>Check result.</returns>
	public static bool IsCodeExtension(this string fileExt)
		=> TryGetCompiler(fileExt) is not null;

	/// <summary>
	/// Determine whether the specified code supports references.
	/// </summary>
	/// <param name="code"><see cref="CodeInfo"/></param>
	/// <returns>Check result.</returns>
	public static bool IsReferencesSupported(this CodeInfo code)
		=> code.CheckOnNull(nameof(code)).Language.IsReferencesSupported();

	/// <summary>
	/// Determines whether the specified file extension supports references.
	/// </summary>
	/// <param name="langExt"><see cref="CodeInfo.Language"/></param>
	/// <returns>Check result.</returns>
	public static bool IsReferencesSupported(this string langExt)
		=> TryGetCompiler(langExt)?.IsReferencesSupported == true;
}