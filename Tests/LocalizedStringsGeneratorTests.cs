namespace StockSharp.Tests;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using StockSharp.Localization;

/// <summary>
/// Drives the localization source generator over resource files that <c>strings.json</c> is allowed
/// to hold. Everything the product says on screen is read through the members this generator emits,
/// so whatever it produces has to be code the consumer can build - parsable, with no member declared
/// twice - and a resource it cannot make sense of has to stop the build rather than quietly hand
/// back a smaller class.
/// </summary>
[TestClass]
public class LocalizedStringsGeneratorTests : BaseTestClass
{
	private sealed class StubAdditionalText(string path, string text) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken)
			=> SourceText.From(text);
	}

	private const string _resourceFileName = "strings.json";
	private const string _generatorFileName = "StockSharp.Localization.Generator.dll";
	private const string _generatorTypeName = "StockSharp.Localization.LocalizedStringsGenerator";
	private const string _generatorBinDir = "../../../../Localization.Generator/bin";

	private static readonly CSharpCompilation _compilation = CSharpCompilation.Create(nameof(LocalizedStringsGeneratorTests));

	private static IIncrementalGenerator _generator;

	private static IIncrementalGenerator Generator => _generator ??= LoadGenerator();

	// The generator is an analyzer of the Localization project, so the test assembly holds no
	// reference to it and it is taken from its own build output, built in the same configuration.
	private static IIncrementalGenerator LoadGenerator()
	{
		var binDir = Path.GetFullPath(_generatorBinDir);

		IsTrue(Directory.Exists(binDir), $"'{binDir}' does not exist: the generator project was not built.");

		var paths = Directory.GetFiles(binDir, _generatorFileName, SearchOption.AllDirectories);

		IsNotEmpty(paths, $"'{_generatorFileName}' was not built.");

		var sep = Path.DirectorySeparatorChar;
		var release = typeof(LocalizedStringsGeneratorTests).Assembly.Location.Contains($"{sep}Release{sep}", StringComparison.OrdinalIgnoreCase);
		var configuration = $"{sep}{(release ? "Release" : "Debug")}{sep}";

		var path = paths.FirstOrDefault(p => p.Contains(configuration, StringComparison.OrdinalIgnoreCase)) ?? paths[0];
		var type = Assembly.LoadFrom(path).GetType(_generatorTypeName);

		IsNotNull(type, $"'{_generatorTypeName}' is not in '{path}'.");

		return (IIncrementalGenerator)Activator.CreateInstance(type);
	}

	// Written by hand rather than through a serializer: a test needs to hand the generator the same
	// awkward text a translator can type, duplicate keys included, which no dictionary can express.
	private static string ToJson(params (string key, string value)[] pairs)
		=> "{" + pairs.Select(p => $"\"{Escape(p.key)}\":\"{Escape(p.value)}\"").Join(",") + "}";

	private static string Escape(string text)
		=> text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");

	private static GeneratorRunResult Run(string json)
	{
		ImmutableArray<AdditionalText> texts = [new StubAdditionalText($"Localization/{_resourceFileName}", json)];

		var results = CSharpGeneratorDriver
			.Create([Generator.AsSourceGenerator()], texts)
			.RunGenerators(_compilation)
			.GetRunResult()
			.Results;

		AreEqual(1, results.Length);
		return results[0];
	}

	private static string GetSource(GeneratorRunResult result)
	{
		IsNull(result.Exception, $"{result.Exception}");
		AreEqual(1, result.GeneratedSources.Length, "expected exactly one generated file");

		return result.GeneratedSources[0].SourceText.ToString();
	}

	private static Diagnostic[] GetParseErrors(string source)
		=> [.. CSharpSyntaxTree.ParseText(source).GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];

	private static void AssertParses(string source)
	{
		var errors = GetParseErrors(source);

		IsEmpty(errors, $"Generated source does not parse: {errors.Select(e => e.ToString()).Join("; ")}{Environment.NewLine}{source}");
	}

	private static IEnumerable<TypeDeclarationSyntax> GetTypeDeclarations(string source)
		=> CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>();

	// Everything a type declaration puts into its own scope: the key constants, the properties, the
	// methods, the cached texts and the type that holds them.
	private static IEnumerable<string> GetMemberNames(MemberDeclarationSyntax member)
		=> member switch
		{
			FieldDeclarationSyntax f => f.Declaration.Variables.Select(v => v.Identifier.ValueText),
			PropertyDeclarationSyntax p => [p.Identifier.ValueText],
			MethodDeclarationSyntax m => [m.Identifier.ValueText],
			TypeDeclarationSyntax t => [t.Identifier.ValueText],
			_ => [],
		};

	// The names one named type declares, and only that type: a property and the cached text behind it
	// share a name by design, and they are members of different types.
	private static string[] GetDeclaredMemberNames(string source, string typeName)
		=> [.. GetTypeDeclarations(source).Where(t => t.Identifier.ValueText == typeName).SelectMany(t => t.Members.SelectMany(GetMemberNames))];

	// A name declared twice by one type is what a duplicate resource looks like from here.
	private static string[] GetDuplicateMemberNames(string source)
		=> [.. GetTypeDeclarations(source).SelectMany(t => t.Members
			.SelectMany(GetMemberNames)
			.GroupBy(n => n, StringComparer.Ordinal)
			.Where(g => g.Count() > 1)
			.Select(g => $"{t.Identifier.ValueText}.{g.Key} x{g.Count()}"))];

	// Whichever way the driver reports a generator that refused its input - the exception it caught,
	// or an error diagnostic in its place - what matters is that the build stops and the text says why.
	private static string RunExpectingRefusal(string json)
	{
		GeneratorRunResult result;

		try
		{
			result = Run(json);
		}
		catch (Exception ex)
		{
			return ex.ToString();
		}

		AreEqual(0, result.GeneratedSources.Length, "a class was generated from a resource the generator cannot read");

		var reasons = new List<string>();

		if (result.Exception is not null)
			reasons.Add(result.Exception.ToString());

		reasons.AddRange(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()));

		return reasons.JoinN();
	}

	/// <summary>
	/// A caller is entitled to reach every resource by name rather than by quoting its key as a
	/// literal: each key in the file yields a constant carrying the key itself and a same-named
	/// property that looks the text up, plus the one reset method that drops those cached texts when
	/// the language changes. This is the whole surface the product compiles against.
	/// </summary>
	[TestMethod]
	public void EveryResourceKeyGetsItsKeyConstantAndItsProperty()
	{
		var source = GetSource(Run(ToJson(("Alpha", "first"), ("Beta", "second"))));

		AssertParses(source);

		var names = GetDeclaredMemberNames(source, nameof(LocalizedStrings));

		Contains(names, "AlphaKey");
		Contains(names, "Alpha");
		Contains(names, "BetaKey");
		Contains(names, "Beta");
		Contains(names, nameof(LocalizedStrings.ResetCache));
	}

	/// <summary>
	/// A resource key is written by whoever adds the string, and nothing stops it from being a C#
	/// keyword, from starting with a digit or from carrying a dot or a hyphen. Whatever the generator
	/// does about such a key - escape it, sanitize it, or leave it out with a diagnostic naming it -
	/// the text it emits has to parse, and the well-formed keys in the same file must keep their
	/// members. Otherwise one new resource breaks the build of every project that reads any string.
	/// </summary>
	[TestMethod]
	public void GeneratedSourceParsesForAwkwardResourceKeys()
	{
		var source = GetSource(Run(ToJson(("class", "a keyword"), ("a-b", "a hyphen"), ("2fa", "a leading digit"), ("Plain", "well formed"))));

		AssertParses(source);

		var names = GetDeclaredMemberNames(source, nameof(LocalizedStrings));

		Contains(names, "PlainKey", $"the well-formed key lost its constant:{Environment.NewLine}{source}");
		Contains(names, "Plain", $"the well-formed key lost its property:{Environment.NewLine}{source}");
	}

	/// <summary>
	/// A translated text may run over several lines - a multi-line tooltip or an error explanation is
	/// ordinary. The value is copied into the documentation comment of the generated member, and a
	/// line break there ends the comment and leaves the rest of the sentence standing in the class
	/// body as code. The emitted text must parse whatever the resource says.
	/// </summary>
	[TestMethod]
	public void GeneratedSourceParsesForAMultiLineResourceValue()
	{
		var source = GetSource(Run(ToJson(("Multi", "first line\nsecond line"), ("Plain", "well formed"))));

		AssertParses(source);

		Contains(GetDeclaredMemberNames(source, nameof(LocalizedStrings)), "Multi");
	}

	/// <summary>
	/// Each key contributes two members, and one of them is named after the key with "Key" appended.
	/// Two resources whose names differ by exactly that suffix therefore aim at the same member name,
	/// and a class cannot declare it twice. The generator owes a distinct member per resource, or the
	/// day somebody adds the second key nothing that reads any localized string compiles.
	/// </summary>
	[TestMethod]
	public void KeySuffixDoesNotCollapseTwoResourcesOntoOneMember()
	{
		var source = GetSource(Run(ToJson(("Foo", "the first"), ("FooKey", "the second"))));

		AssertParses(source);

		var duplicates = GetDuplicateMemberNames(source);

		IsEmpty(duplicates, $"Duplicate members: {duplicates.Join(", ")}{Environment.NewLine}{source}");
	}

	/// <summary>
	/// The emitted class declares members of its own - the reset method and the cache the properties
	/// answer from - and a resource key is free to be spelled exactly like one of them. The resource
	/// gets a member of its own rather than colliding with the machinery, which would otherwise be a
	/// build the whole product cannot complete over one added string.
	/// </summary>
	[TestMethod]
	public void ResourceNamedAfterTheMachineryGetsAMemberOfItsOwn()
	{
		var source = GetSource(Run(ToJson(("Cache", "a word"), ("ResetCache", "another word"), ("Plain", "well formed"))));

		AssertParses(source);

		var duplicates = GetDuplicateMemberNames(source);

		IsEmpty(duplicates, $"Duplicate members: {duplicates.Join(", ")}{Environment.NewLine}{source}");

		var names = GetDeclaredMemberNames(source, nameof(LocalizedStrings));
		var keyConstants = names.Where(n => n.EndsWith("Key", StringComparison.Ordinal)).ToArray();

		HasCount(3, keyConstants, $"a resource lost its key constant: {keyConstants.Join(", ")}");
		Contains(names, "Plain", $"the resource that shares no name with the machinery lost its property:{Environment.NewLine}{source}");
	}

	/// <summary>
	/// JSON lets the same key appear twice, and a merge that lands two copies of a resource is exactly
	/// how it happens. Only one of the two texts can win, so the build must stop and name the key
	/// rather than pick one - a silently chosen translation is a wrong word on screen nobody looks for.
	/// </summary>
	[TestMethod]
	public void DuplicateResourceKeyStopsTheBuildAndNamesTheKey()
	{
		const string key = "Doubled";

		var reported = RunExpectingRefusal(ToJson((key, "the first"), (key, "the second"), ("Plain", "well formed")));

		IsNotEmpty(reported, "a resource declaring the same key twice was accepted without a word");
		IsTrue(reported.Contains(key, StringComparison.Ordinal), $"the report does not name the duplicated key: {reported}");
	}

	/// <summary>
	/// A resource file that is not valid JSON - a truncated merge, a stray comma - must stop the build.
	/// Emitting an empty class instead would compile perfectly and turn every label in the product
	/// into its own resource key, which is the kind of failure that reaches a user rather than a build log.
	/// </summary>
	[TestMethod]
	public void MalformedResourceStopsTheBuildInsteadOfEmittingAnEmptyClass()
	{
		var reported = RunExpectingRefusal("{ \"Plain\": \"well formed\", }");

		IsNotEmpty(reported, "a malformed resource file was accepted without a word");
	}
}
