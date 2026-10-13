namespace StockSharp.Tests;

using System.Text.RegularExpressions;

using StockSharp.Localization;
using StockSharp.Media;

/// <summary>
/// Consumer contract for the generated <see cref="MediaNames"/> constants: every constant must
/// address exactly one file that ships in the Media project, and the URI built from it by
/// <see cref="MediaIconAttribute"/> must carry that file name unchanged.
/// </summary>
[TestClass]
public class MediaNamesTests : BaseTestClass
{
	// The logos are WPF resources of the Windows-only Media project, which a net10.0 test project
	// cannot reference. The folder is the single source both csproj files read - Media.csproj as
	// <Resource>, Media.Names.csproj as <AdditionalFiles> - so it is the source of truth here.
	private const string _logosFolder = "../../../../Media/logos";

	private static string[] GetLogoFiles()
	{
		Directory.Exists(_logosFolder).AssertTrue($"Logos folder '{Path.GetFullPath(_logosFolder)}' not found.");

		// Filtered by extension rather than by a search pattern: on Windows "*.svg" also matches
		// longer extensions through 8.3 short names.
		var files = Directory
			.GetFiles(_logosFolder)
			.Where(f => f.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
			.Select(Path.GetFileName)
			.ToArray();

		(files.Length > 0).AssertTrue($"No svg files in '{Path.GetFullPath(_logosFolder)}'.");

		return files;
	}

	private static FieldInfo[] GetConstantFields()
		=> typeof(MediaNames).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

	private static Dictionary<string, string> GetConstants()
		=> GetConstantFields().ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue(), StringComparer.Ordinal);

	// Consumers write [MediaIcon(MediaNames.fix)], and an attribute argument must be a compile-time
	// constant - a static readonly field would still build here but break every consumer.
	[TestMethod]
	public void EveryConstantIsACompileTimeString()
	{
		var fields = GetConstantFields();

		(fields.Length > 0).AssertTrue($"{nameof(MediaNames)} exposes no constants.");

		foreach (var field in fields)
		{
			field.IsLiteral.AssertTrue($"{nameof(MediaNames)}.{field.Name} is not a const.");
			field.FieldType.AssertEqual(typeof(string), $"{nameof(MediaNames)}.{field.Name} is not a string.");
		}
	}

	// A constant whose file was renamed or deleted still compiles and only fails at runtime as a
	// missing icon, so the file has to be matched here - ordinally, because the value is pasted
	// verbatim into the resource URI.
	[TestMethod]
	public void EveryConstantNamesAnExistingLogoFile()
	{
		var files = GetLogoFiles().ToHashSet(StringComparer.Ordinal);

		foreach (var (name, value) in GetConstants())
			files.Contains(value).AssertTrue($"{nameof(MediaNames)}.{name} = '{value}' has no file in '{Path.GetFullPath(_logosFolder)}'.");
	}

	// The other direction of the same contract: a shipped logo that no constant addresses is
	// unreachable through MediaNames, and two constants pointing at one file mean the naming rule
	// collapsed two different logos into one member.
	[TestMethod]
	public void EveryLogoFileHasExactlyOneConstant()
	{
		var files = GetLogoFiles();
		var constants = GetConstants();

		var perFile = constants
			.GroupBy(p => p.Value, StringComparer.Ordinal)
			.ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

		foreach (var file in files)
		{
			perFile.TryGetValue(file, out var count);
			count.AssertEqual(1, $"'{file}' is addressed by {count} constants.");
		}

		constants.Count.AssertEqual(files.Length, "Constant count and logo file count differ.");
	}

	// The catalogue is addressed by hand in source (MediaNames.fix), so the member names are part of
	// the public contract: lower-case, no digits-first, nothing that needs @-escaping.
	[TestMethod]
	public void ConstantNamesAreLowerCaseIdentifiers()
	{
		var pattern = new Regex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant);

		foreach (var name in GetConstants().Keys)
			pattern.IsMatch(name).AssertTrue($"{nameof(MediaNames)}.{name} is not a lower-case identifier.");
	}

	// The only constant an in-repo consumer spells out (DefaultFixDialect). Computed by hand from the
	// naming rule: file 'fix_logo.svg' -> strip the extension -> strip the '_logo' suffix -> 'fix'.
	[TestMethod]
	public void FixDialectLogoIsAddressableByName()
	{
		MediaNames.fix.AssertEqual("fix_logo.svg");

		GetLogoFiles().Contains("fix_logo.svg", StringComparer.Ordinal).AssertTrue();
	}

	// WPF resolves the pack URI by the resource path, so the last segment has to be the file name
	// byte-for-byte - a lower-cased or otherwise rewritten segment silently resolves to nothing.
	[TestMethod]
	public void MediaIconUriCarriesTheFileNameUnchanged()
	{
		foreach (var (name, value) in GetConstants())
		{
			var attr = new MediaIconAttribute(value);

			attr.Icon.AssertEqual($"/StockSharp.Media;component/logos/{value}", $"Wrong URI for {nameof(MediaNames)}.{name}.");
			attr.IsFullPath.AssertTrue($"URI for {nameof(MediaNames)}.{name} is not marked as a full path.");
		}
	}

	// Media.Names.csproj feeds only *.svg to the generator while the generator itself also accepts
	// PNG, so a PNG dropped into the folder would ship as a WPF resource with no constant addressing
	// it. Until both csproj files agree on the wider set, the folder must stay svg-only.
	[TestMethod]
	public void LogosFolderContainsOnlySvg()
	{
		Directory.Exists(_logosFolder).AssertTrue($"Logos folder '{Path.GetFullPath(_logosFolder)}' not found.");

		var others = Directory
			.GetFiles(_logosFolder)
			.Where(f => !f.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
			.Select(Path.GetFileName)
			.ToArray();

		others.Length.AssertEqual(0, $"Not addressable by MediaNames: {others.Join(", ")}");
	}
}
