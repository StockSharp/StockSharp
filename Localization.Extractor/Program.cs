namespace StockSharp.Localization.Extractor;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

using Ecng.Serialization;
using Ecng.Common;
using Ecng.Collections;

using Spectre.Console;
using Spectre.Console.Cli;

static class Program
{
	private static readonly string _gitHubPah = @"..\..\..\..\";
	private const string _locPref = "Localization";
	private static readonly string _locPath = $@"{_gitHubPah}{_locPref}\";
	private const string _langsPref = ".Langs";
	private static readonly string _langsPath = $@"{_gitHubPah}{_locPref}{_langsPref}\";
	private static readonly string _stringsFile = "strings.json";
	private const string _en = "en";
	private static readonly string[] _langs = new[] { _en }.Concat([.. Directory.GetDirectories(_langsPath).Select(s => new DirectoryInfo(s).Name).Where(s => s.Length == 2)]);

	public static Task Main(string[] args)
	{
		var app = new CommandApp();

		app.Configure(config =>
		{
			config.SetApplicationName(Assembly.GetEntryAssembly()?.GetName().Name ?? nameof(Extractor));

			config.AddCommand<SortCommand>("sort")
				  .WithDescription("Sorts localization strings in JSON files")
				  .WithExample(["sort"]);

			config.AddCommand<SplitCommand>("split")
				  .WithDescription("Splits a specified JSON file into language-specific strings")
				  .WithExample(["split"]);

			config.AddCommand<ValidateCommand>("validate")
				  .WithDescription("Validates of localization strings across languages")
				  .WithExample(["validate"]);
		});

		return app.RunAsync(args);
	}

	private static string FixEnPath(string path)
		=> path.Replace($"{_langsPref}\\{_en}", "");

	private class SortCommand : AsyncCommand
	{
		public override Task<int> ExecuteAsync(CommandContext context)
		{
			AnsiConsole.MarkupLine("[bold green]Localization Utility: Sort[/]");

			AnsiConsole.Status()
				.Start("Sorting strings...", ctx =>
				{
					foreach (var lang in _langs)
					{
						var file = FixEnPath($@"{_langsPath}{lang}\{_stringsFile}");
						AnsiConsole.MarkupLine($"Sorting [cyan]{lang}[/] strings...");

						var strings = File.ReadAllText(file).DeserializeObject<IDictionary<string, string>>();
						var ordered = strings.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
						File.WriteAllText(file, ordered.ToJson());
					}
				});

			AnsiConsole.MarkupLine("[green]Sort completed successfully.[/]");
			return Task.FromResult(0);
		}
	}

	class SplitCommandSettings : CommandSettings
	{
		[CommandArgument(0, "<jsonFile>")]
		public string JsonFile { get; set; }

		public override ValidationResult Validate()
		{
			if (!File.Exists(JsonFile))
				return ValidationResult.Error($"JSON file '{JsonFile}' does not exist.");

			return base.Validate();
		}
	}

	private class SplitCommand : AsyncCommand<SplitCommandSettings>
	{
		public override Task<int> ExecuteAsync(CommandContext context, SplitCommandSettings settings)
		{
			AnsiConsole.MarkupLine("[bold green]Localization Utility: Split[/]");

			AnsiConsole.Status()
				.Start("Splitting files...", ctx =>
				{
					var fileName = Path.GetFileNameWithoutExtension(settings.JsonFile);
					AnsiConsole.MarkupLine($"Processing [cyan]{fileName}.json[/]...");

					var translates = File.ReadAllText(settings.JsonFile).DeserializeObject<IDictionary<string, IDictionary<string, string>>>();

					foreach (var (name, langDict) in translates)
					{
						foreach (var (lang, value) in langDict)
						{
							var langFile = FixEnPath($@"{_langsPath}{lang}\{_stringsFile}");

							var strings = new SortedDictionary<string, string>();
							strings.AddRange(File.ReadAllText(langFile).DeserializeObject<IDictionary<string, string>>());

							strings[name] = value;

							File.WriteAllText(langFile, strings.ToJson());
						}
					}
				});

			AnsiConsole.MarkupLine("[green]Split completed successfully.[/]");
			return Task.FromResult(0);
		}
	}

	private class ValidateCommand : AsyncCommand
	{
		public override Task<int> ExecuteAsync(CommandContext context)
		{
			AnsiConsole.MarkupLine("[bold green]Localization Utility: Validate[/]");

			AnsiConsole.Status()
				.Start("Validating string counts...", ctx =>
				{
					var lines = File.ReadAllText($@"{_locPath}{_stringsFile}").DeserializeObject<IDictionary<string, string>>();
					var cnt = lines.Count;

					foreach (var lang in _langs)
					{
						var file = FixEnPath($@"{_langsPath}{lang}\{_stringsFile}");
						var strings = File.ReadAllText(file).DeserializeObject<IDictionary<string, string>>();

						if (cnt != strings.Count)
						{
							AnsiConsole.MarkupLine($"[red]Count mismatch: {cnt} (base) != {strings.Count} ({lang})[/]");
						}
					}
				});

			AnsiConsole.MarkupLine("[green]Validate completed successfully.[/]");
			return Task.FromResult(0);
		}
	}
}