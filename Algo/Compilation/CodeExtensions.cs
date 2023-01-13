namespace StockSharp.Algo.Compilation
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Compilation;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class CodeExtensions
	{
		/// <summary>
		/// To compile the code.
		/// </summary>
		/// <param name="compiler">Compiler.</param>
		/// <param name="code">Code.</param>
		/// <param name="name">The reference name.</param>
		/// <param name="references">References.</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
		/// <returns>The result of the compilation.</returns>
		public static CompilationResult CompileCode(this ICompiler compiler, string code, string name, IEnumerable<CodeReference> references, CancellationToken cancellationToken = default)
			=> compiler.Compile(new(), name, code, references.Where(r => r.IsValid).Select(r => r.FullLocation).ToArray(), cancellationToken);

		private static readonly IEnumerable<string> _defaultReferences = new[]
		{
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

			"Ecng.Common",
			"Ecng.Collections",
			"Ecng.ComponentModel",
			"Ecng.Configuration",
			"Ecng.Localization",
			"Ecng.Serialization",

			"StockSharp.Algo",
			"StockSharp.Messages",
			"StockSharp.BusinessEntities",
			"StockSharp.Logging",
			"StockSharp.Localization",
			"StockSharp.Diagram.Core",
			"StockSharp.Charting.Interfaces",
			"StockSharp.Alerts.Interfaces",
		};

		/// <summary>
		/// Default references.
		/// </summary>
		public static IEnumerable<CodeReference> DefaultReferences
			=>	_defaultReferences
				.Select(r => new CodeReference { Location = $"{r}.dll" })
				.ToArray();
	}
}