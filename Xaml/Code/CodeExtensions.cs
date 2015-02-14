namespace StockSharp.Xaml.Code
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;

	/// <summary>
	/// Вспомогательный класс.
	/// </summary>
	public static class CodeExtensions
	{
		/// <summary>
		/// Скомпилировать код.
		/// </summary>
		/// <param name="language">Язык программирования.</param>
		/// <param name="code">Код.</param>
		/// <param name="name">Имя сборки.</param>
		/// <param name="references">Ссылки.</param>
		/// <param name="outDir">Директория, куда будет сохранена скомпилированная сборка.</param>
		/// <param name="tempPath">Временная директория.</param>
		/// <returns>Результат компиляции.</returns>
		public static CompilationResult CompileCode(this CompilationLanguages language, string code, string name, IEnumerable<CodeReference> references, string outDir, string tempPath)
		{
			return Compiler.Create(language, outDir, tempPath)
				.Compile(name, code, references.Select(s => s.Location).ToArray());
		}

		/// <summary>
		/// Есть ли ошибки в результате компиляции.
		/// </summary>
		/// <param name="result">Результат компиляции.</param>
		/// <returns><see langword="true"/> - если ошибки есть, <see langword="true"/> - если компиляция выполнена без ошибок.</returns>
		public static bool HasErrors(this CompilationResult result)
		{
			return result.Errors.Any(e => e.Type == CompilationErrorTypes.Error);
		}

		private static readonly string[] _defaultReferences =
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
		/// Сборки по-умолчанию.
		/// </summary>
		public static IEnumerable<string> DefaultReferences
		{
			get { return _defaultReferences; }
		}

		/// <summary>
		/// Преобразовать имя сборки в <see cref="CodeReference"/>.
		/// </summary>
		/// <param name="referenceName">Имя сборки.</param>
		/// <param name="assemblies">Ранее загруженные сборки.</param>
		/// <returns><see cref="CodeReference"/>.</returns>
		public static CodeReference ToReference(this string referenceName, Assembly[] assemblies)
		{
			if (referenceName.IsEmpty())
				throw new ArgumentNullException("referenceName");

			if (assemblies == null)
				throw new ArgumentNullException("assemblies");

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
		/// Преобразовать имена сборок в <see cref="CodeReference"/>.
		/// </summary>
		/// <param name="referenceNames">Имена сборок.</param>
		/// <returns><see cref="CodeReference"/>.</returns>
		public static IEnumerable<CodeReference> ToReferences(this IEnumerable<string> referenceNames)
		{
			if (referenceNames == null)
				throw new ArgumentNullException("referenceNames");

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			return referenceNames
				.Select(r => ToReference(r, assemblies))
				.Where(r => r != null)
				.ToArray();
		}
	}
}