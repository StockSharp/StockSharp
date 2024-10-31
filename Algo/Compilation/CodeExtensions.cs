namespace StockSharp.Algo.Compilation;

using Ecng.Compilation;

/// <summary>
/// Extension class.
/// </summary>
public static class CodeExtensions
{
	private static readonly IEnumerable<string> _defaultReferences =
	[
		"mscorlib",
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
	public static IEnumerable<CodeReference> DefaultReferences
		=>	_defaultReferences
			.Select(r => new CodeReference { Location = $"{r}.dll" })
			.ToArray();

	/// <summary>
	/// Throw if errors.
	/// </summary>
	/// <param name="res"><see cref="CompilationResult"/></param>
	/// <returns><see cref="CompilationResult"/></returns>
	public static CompilationResult ThrowIfErrors(this CompilationResult res)
	{
		if (res.HasErrors())
			throw new InvalidOperationException($"Compilation error: {res.Errors.Where(e => e.Type == CompilationErrorTypes.Error).Take(2).Select(e => e.ToString()).JoinN()}");

		return res;
	}

	/// <summary>
	/// Try to find type.
	/// </summary>
	/// <param name="asm">Assembly.</param>
	/// <param name="isTypeCompatible">Is type compatible.</param>
	/// <param name="typeName">Type name.</param>
	/// <returns>Found type.</returns>
	public static Type TryFindType(this Assembly asm, Func<Type, bool> isTypeCompatible, string typeName)
	{
		if (asm is null)
			throw new ArgumentNullException(nameof(asm));

		if (isTypeCompatible is null && typeName.IsEmpty())
			throw new ArgumentNullException(nameof(typeName));

		if (!typeName.IsEmpty())
			return asm.GetType(typeName, false, true);
		else
			return asm.GetTypes().FirstOrDefault(isTypeCompatible);
	}
}