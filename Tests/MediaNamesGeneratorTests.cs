namespace StockSharp.Tests;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using StockSharp.Media;

/// <summary>
/// Drives <see cref="MediaNamesGenerator"/> over file name sets that the logos folder is allowed to
/// contain. Whatever the generator emits has to be code the consumer can build: parsable, with no
/// member declared twice, and identical whatever locale the build machine runs in.
/// </summary>
[TestClass]
[DoNotParallelize] // one test switches the thread culture
public class MediaNamesGeneratorTests : BaseTestClass
{
	private sealed class StubAdditionalText(string path) : AdditionalText
	{
		public override string Path { get; } = path;

		// The generator looks only at the path, so the content stays empty.
		public override SourceText GetText(CancellationToken cancellationToken)
			=> SourceText.From(string.Empty);
	}

	private static readonly CSharpCompilation _compilation = CSharpCompilation.Create(nameof(MediaNamesGeneratorTests));

	private static GeneratorDriver CreateDriver(IEnumerable<string> paths)
		=> CSharpGeneratorDriver.Create([new MediaNamesGenerator().AsSourceGenerator()], ToTexts(paths));

	private static ImmutableArray<AdditionalText> ToTexts(IEnumerable<string> paths)
		=> [.. paths.Select(p => (AdditionalText)new StubAdditionalText(p))];

	private static GeneratorRunResult Run(params string[] paths)
	{
		var results = CreateDriver(paths).RunGenerators(_compilation).GetRunResult().Results;
		results.Length.AssertEqual(1);
		return results[0];
	}

	private static string GetSource(GeneratorRunResult result)
	{
		result.GeneratedSources.Length.AssertEqual(1, "Expected exactly one generated file.");
		return result.GeneratedSources[0].SourceText.ToString();
	}

	private static VariableDeclaratorSyntax[] GetDeclarators(string source)
		=> [.. CSharpSyntaxTree
			.ParseText(source)
			.GetRoot()
			.DescendantNodes()
			.OfType<FieldDeclarationSyntax>()
			.SelectMany(f => f.Declaration.Variables)];

	private static string[] GetDeclaredNames(string source)
		=> [.. GetDeclarators(source).Select(v => v.Identifier.ValueText)];

	// A file name is not a C# identifier: a hyphen, a leading digit or a keyword all turn into
	// members that do not parse, and the whole consuming project stops building. The generator has
	// to produce something legal - escaped, sanitized or skipped with a diagnostic - either way the
	// emitted text must parse, and the well-named file in the same batch must still get its constant.
	[TestMethod]
	public void GeneratedSourceParsesForAwkwardFileNames()
	{
		var source = GetSource(Run("logos/a-b.svg", "logos/123.svg", "logos/class.svg", "logos/plain_logo.svg"));

		var errors = CSharpSyntaxTree
			.ParseText(source)
			.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		errors.Length.AssertEqual(0, $"Generated source does not parse: {errors.Select(e => e.ToString()).Join("; ")}{Environment.NewLine}{source}");

		GetDeclaredNames(source).Contains("plain", StringComparer.Ordinal).AssertTrue($"'plain_logo.svg' lost its constant:{Environment.NewLine}{source}");
	}

	/// <summary>
	/// A file name is not an identifier either: a space, a version dot and a plus sign all travel into
	/// the member name unchanged. Whatever the generator decides to do about them - replace the
	/// character or leave the file out with a diagnostic - every constant it does declare must be a
	/// name C# can bind, and no image may vanish without one. Otherwise dropping a logo into the
	/// folder with a space in its name stops the consuming project from building.
	/// </summary>
	[TestMethod]
	public void FileNameWithSeparatorsBecomesAValidIdentifier()
	{
		string[] files = ["logos/my logo.svg", "logos/acme.v2.svg", "logos/a+b.png"];

		var source = GetSource(Run(files));

		var errors = CSharpSyntaxTree
			.ParseText(source)
			.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		errors.Length.AssertEqual(0, $"Generated source does not parse: {errors.Select(e => e.ToString()).Join("; ")}{Environment.NewLine}{source}");

		var names = GetDeclaredNames(source);

		var illegal = names
			.Where(n => !SyntaxFacts.IsValidIdentifier(n) || SyntaxFacts.GetKeywordKind(n) != SyntaxKind.None)
			.ToArray();

		illegal.Length.AssertEqual(0, $"Not usable as member names: {illegal.Join(", ")}{Environment.NewLine}{source}");
		names.Length.AssertEqual(files.Length, $"An image lost its constant:{Environment.NewLine}{source}");
	}

	// Different resources must not collapse onto one member: 'x.svg' and 'x_logo.svg' both reduce to
	// 'x' once the suffix is stripped, 'x.png' adds a third, and an equally named file from another
	// folder a fourth. Whichever way the clash is resolved, the class cannot declare 'x' twice.
	[TestMethod]
	public void ConstantNamesAreUnique()
	{
		var source = GetSource(Run("logos/x.svg", "logos/x_logo.svg", "logos/x.png", "other/x.svg"));

		var names = GetDeclaredNames(source);

		var duplicates = names
			.GroupBy(n => n, StringComparer.Ordinal)
			.Where(g => g.Count() > 1)
			.Select(g => $"{g.Key} x{g.Count()}")
			.ToArray();

		duplicates.Length.AssertEqual(0, $"Duplicate constants: {duplicates.Join(", ")}{Environment.NewLine}{source}");
	}

	// The member name is lower-cased for the consumer, but the value is what gets pasted into the
	// resource URI, so it must stay the file name exactly, extension case included.
	[TestMethod]
	public void ConstantValueIsTheFileNameVerbatim()
	{
		var source = GetSource(Run("logos/Upper_Logo.SVG", "logos/mixed.PNG"));

		var constants = GetDeclarators(source)
			.ToDictionary(v => v.Identifier.ValueText, v => ((LiteralExpressionSyntax)v.Initializer.Value).Token.ValueText, StringComparer.Ordinal);

		constants.Count.AssertEqual(2, source);
		constants["upper"].AssertEqual("Upper_Logo.SVG");
		constants["mixed"].AssertEqual("mixed.PNG");
	}

	// With nothing to generate the consumer must be told, not handed an empty class.
	[TestMethod]
	public void NoIconsReportsWarningAndEmitsNothing()
	{
		var result = Run();

		result.GeneratedSources.Length.AssertEqual(0);
		result.Diagnostics.Count(d => d.Id == "MEDIA001" && d.Severity == DiagnosticSeverity.Warning).AssertEqual(1);
	}

	// AdditionalFiles carry more than logos (release-tracking markdown, for one, and a licence file
	// with no extension at all). Anything that is not a PNG or an SVG must be invisible to the
	// generator - reading it as an image would name a constant after a file no resource URI resolves.
	[TestMethod]
	public void NonImageAdditionalFilesAreIgnored()
	{
		var result = Run("logos/readme.md", "Analyzers/AnalyzerReleases.Shipped.md", "logos/notes.txt", "logos/LICENSE");

		result.GeneratedSources.Length.AssertEqual(0);
		result.Diagnostics.Count(d => d.Id == "MEDIA001").AssertEqual(1);
	}

	// Generated code must be a function of the inputs alone. Czech collation sorts the 'ch' digraph
	// after 'h', so a culture-sensitive sort reshuffles these three files on a Czech machine and the
	// same commit produces two different files. Expected order is the ordinal one: cha < da < ha.
	[TestMethod]
	public void GeneratedOrderDoesNotDependOnCurrentCulture()
	{
		string[] files = ["logos/cha_logo.svg", "logos/da_logo.svg", "logos/ha_logo.svg"];
		string[] expected = ["cha", "da", "ha"];

		var saved = CultureInfo.CurrentCulture;

		string czech;
		string invariant;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
			czech = GetSource(Run(files));

			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			invariant = GetSource(Run(files));
		}
		finally
		{
			CultureInfo.CurrentCulture = saved;
		}

		GetDeclaredNames(invariant).Join(",").AssertEqual(expected.Join(","));
		czech.AssertEqual(invariant, $"Generated source depends on the current culture:{Environment.NewLine}{czech}");
	}

	// Deleting a logo has to take its constant with it, otherwise the incremental pipeline keeps
	// handing consumers a name whose resource is gone.
	[TestMethod]
	public void RemovedFileDropsItsConstant()
	{
		var driver = CreateDriver(["logos/a_logo.svg", "logos/b_logo.svg"]);

		driver = driver.RunGenerators(_compilation);
		GetDeclaredNames(GetSource(driver.GetRunResult().Results[0])).Join(",").AssertEqual("a,b");

		driver = driver.ReplaceAdditionalTexts(ToTexts(["logos/a_logo.svg"]));

		driver = driver.RunGenerators(_compilation);
		GetDeclaredNames(GetSource(driver.GetRunResult().Results[0])).Join(",").AssertEqual("a");
	}
}
