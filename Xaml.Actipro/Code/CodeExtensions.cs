namespace StockSharp.Xaml.Actipro.Code
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class CodeExtensions
	{
		/// <summary>
		/// To compile the code.
		/// </summary>
		/// <param name="language">Programming language.</param>
		/// <param name="code">Code.</param>
		/// <param name="name">The build name.</param>
		/// <param name="references">References.</param>
		/// <param name="outDir">The directory where the compiled build will be saved.</param>
		/// <param name="tempPath">The temporary directory.</param>
		/// <returns>The result of the compilation.</returns>
		public static CompilationResult CompileCode(this CompilationLanguages language, string code, string name, IEnumerable<CodeReference> references, string outDir, string tempPath)
		{
			return Compiler.Create(language, outDir, tempPath)
				.Compile(name, code, references.Select(s => s.Location).ToArray());
		}

		/// <summary>
		/// Are there any errors in the compilation.
		/// </summary>
		/// <param name="result">The result of the compilation.</param>
		/// <returns><see langword="true" /> - If there are errors, <see langword="true" /> - If the compilation is performed without errors.</returns>
		public static bool HasErrors(this CompilationResult result)
		{
			return result.Errors.Any(e => e.Type == CompilationErrorTypes.Error);
		}

		/// <summary>
		/// Default builds.
		/// </summary>
		public static IEnumerable<string> DefaultReferences => new[]
		{
			"System",
			"System.Core",
			"System.Configuration",
			"System.Data",
			"System.Xaml",
			"System.Xml",
			"System.Xaml",
			"WindowsBase",
			"PresentationCore",
			"PresentationFramework",

			"Ecng.Common",
			"Ecng.Collections",
			"Ecng.ComponentModel",
			"Ecng.Configuration",
			"Ecng.Localization",
			"Ecng.Serialization",
			"Ecng.Xaml",

			"MoreLinq",
			"MathNet.Numerics",

			"StockSharp.Algo",
			"StockSharp.Algo.Strategies",
			"StockSharp.Algo.History",
			"StockSharp.Messages",
			"StockSharp.BusinessEntities",
			"StockSharp.Logging",
			"StockSharp.Localization",
			"StockSharp.Xaml",
			"StockSharp.Xaml.Charting",
			"StockSharp.Xaml.Diagram",

			"Abt.Controls.SciChart.Wpf",
			"Xceed.Wpf.Toolkit"
		};

		/// <summary>
		/// To modify the build name to <see cref="CodeReference"/>.
		/// </summary>
		/// <param name="referenceName">The build name.</param>
		/// <param name="assemblies">Previously loaded builds.</param>
		/// <returns><see cref="CodeReference"/>.</returns>
		public static CodeReference ToReference(this string referenceName, Assembly[] assemblies)
		{
			if (referenceName.IsEmpty())
				throw new ArgumentNullException(nameof(referenceName));

			if (assemblies == null)
				throw new ArgumentNullException(nameof(assemblies));

			var asm = assemblies.FirstOrDefault(a => a.ManifestModule.Name == referenceName + ".dll");

			if (asm == null)
			{
				try
				{
					asm = Assembly.Load(referenceName);
				}
				catch (FileNotFoundException)
				{
					return null;
				}
			}

			return new CodeReference
			{
				Name = referenceName,
				Location = asm.Location
			};
		}

		/// <summary>
		/// To modify build names to the <see cref="CodeReference"/>.
		/// </summary>
		/// <param name="referenceNames">Build names.</param>
		/// <returns><see cref="CodeReference"/>.</returns>
		public static IEnumerable<CodeReference> ToReferences(this IEnumerable<string> referenceNames)
		{
			if (referenceNames == null)
				throw new ArgumentNullException(nameof(referenceNames));

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			return referenceNames
				.Select(r => ToReference(r, assemblies))
				.Where(r => r != null)
				.ToArray();
		}
	}
}