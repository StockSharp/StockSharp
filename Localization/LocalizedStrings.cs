namespace StockSharp.Localization;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Ecng.Common;
using Ecng.Serialization;

/// <summary>
/// Localized strings.
/// </summary>
public static partial class LocalizedStrings
{
	private class EcngLocalizer : Ecng.Localization.ILocalizer
	{
		public string Localize(string enStr)
			=> enStr.Translate();

		public string LocalizeByKey(string key)
			=> GetString(key);
	}

	private class Translation
	{
		private readonly Dictionary<string, string> _stringsById;
		private readonly Dictionary<string, string> _idsByString;

		public Translation(IDictionary<string, string> strings)
		{
			_stringsById = new(strings.Count);
			_idsByString = new(strings.Count);

			foreach (var pair in strings)
			{
				_stringsById.Add(pair.Key, pair.Value);
				_idsByString[pair.Value] = pair.Key;
			}
		}

		public string GetTextById(string id) => _stringsById.TryGetValue(id, out var text) ? text : null;
		public string GetIdByText(string text) => _idsByString.TryGetValue(text, out var id) ? id : null;
	}

	private static readonly Lock _sync = new();

	// The registry is read from every thread that puts a word on screen and written whenever a language
	// pack is registered or dropped. Writers build the next map and publish it in one assignment under
	// _sync, and never edit a published one; a reader takes the map once and reads that one to the end.
	// A language is addressed by its code throughout, so nothing is renumbered when another one goes.
	private static volatile Dictionary<string, Translation> _translations = new(StringComparer.InvariantCultureIgnoreCase);

	static LocalizedStrings()
	{
		try
		{
			static Stream extractResource(Assembly asm)
				=> asm.CheckOnNull(nameof(asm)).GetManifestResourceStream($"{asm.GetName().Name}.{_stringsFileName}");

			var mainAsm = typeof(LocalizedStrings).Assembly;
			AddLanguage(EnCode, extractResource(mainAsm));

			foreach (var resFile in Directory.GetFiles(global::System.IO.Path.GetDirectoryName(mainAsm.Location), "StockSharp.Localization.*.dll"))
			{
				try
				{
					var lang = global::System.IO.Path.GetFileNameWithoutExtension(resFile).Remove("StockSharp.Localization.", true);

					if (lang.Length != 2)
						continue;

					var stream = extractResource(global::System.Reflection.Assembly.LoadFrom(resFile));

					if (stream is not null)
						AddLanguage(lang, stream);
				}
				catch (Exception ex)
				{
					Trace.WriteLine(ex);
				}
			}

			Ecng.Localization.LocalizedStrings.Localizer = new EcngLocalizer();
		}
		catch (Exception ex)
		{
			Trace.WriteLine(InitError = ex);
		}
	}

	/// <summary>
	/// Add language.
	/// </summary>
	/// <param name="langCode">Language.</param>
	/// <param name="stream">Resource stream.</param>
	public static void AddLanguage(string langCode, Stream stream)
	{
		using var reader = new StreamReader(stream);
		AddLanguage(langCode, reader.ReadToEnd().DeserializeObject<IDictionary<string, string>>());
	}

	/// <summary>
	/// Add language.
	/// </summary>
	/// <param name="langCode">Language.</param>
	/// <param name="strings">Localized strings.</param>
	public static void AddLanguage(string langCode, IDictionary<string, string> strings)
	{
		if (langCode.IsEmpty())
			throw new ArgumentNullException(nameof(langCode));

		if (strings is null)
			throw new ArgumentNullException(nameof(strings));

		var translation = new Translation(strings);
		bool activeChanged;

		using (_sync.EnterScope())
		{
			var next = new Dictionary<string, Translation>(_translations, StringComparer.InvariantCultureIgnoreCase);

			// A code that is already registered throws here, before anything is published.
			next.Add(langCode, translation);

			_translations = next;

			// the language the caller asked for is carried again, so texts cached from the fallback are stale.
			activeChanged = _requestedLanguage.EqualsIgnoreCase(langCode);

			if (activeChanged)
				ResetCache();
		}

		// A handler relabels what is already on screen and may take locks of its own, so it runs with _sync released.
		if (activeChanged)
			ActiveLanguageChanged?.Invoke();
	}

	/// <summary>
	/// Remove language.
	/// </summary>
	/// <param name="langCode">Language.</param>
	/// <returns>Operation result.</returns>
	public static bool RemoveLanguage(string langCode)
	{
		if (langCode.IsEmpty())
			throw new ArgumentNullException(nameof(langCode));

		bool activeChanged;

		using (_sync.EnterScope())
		{
			if (!_translations.ContainsKey(langCode))
				return false;

			var next = new Dictionary<string, Translation>(_translations, StringComparer.InvariantCultureIgnoreCase);
			next.Remove(langCode);

			_translations = next;

			// the language being read is gone, so every text cached from it goes with it.
			activeChanged = _requestedLanguage.EqualsIgnoreCase(langCode);

			if (activeChanged)
				ResetCache();
		}

		if (activeChanged)
			ActiveLanguageChanged?.Invoke();

		return true;
	}

	/// <summary>
	/// Russian language.
	/// </summary>
	public const string RuCode = "ru";

	/// <summary>
	/// English language.
	/// </summary>
	public const string EnCode = "en";

	/// <summary>
	/// Get all available languages.
	/// </summary>
	public static IEnumerable<string> LangCodes => _translations.Keys;

	/// <summary>
	/// Initialization error.
	/// </summary>
	public static Exception InitError { get; }

	/// <summary>
	/// Error handler to track missed translations or resource keys.
	/// </summary>
	public static event Action<string, bool> Missing;

	/// <summary>
	/// <see cref="ActiveLanguage"/> changed event.
	/// </summary>
	public static event Action ActiveLanguageChanged;

	private static volatile string _requestedLanguage = EnCode;

	/// <summary>
	/// Current language. It is the language chosen by the caller for as long as that language is
	/// registered; while it is not, a registered one answers in its place, so no lookup is made
	/// against a language that carries no words.
	/// </summary>
	public static string ActiveLanguage
	{
		get => GetActiveLanguage(_translations);
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			using (_sync.EnterScope())
			{
				if (_requestedLanguage.EqualsIgnoreCase(value) || !_translations.ContainsKey(value))
					return;

				_requestedLanguage = value;
				ResetCache();
			}

			try
			{
				var cultureInfo = CurrentCulture;

				Thread.CurrentThread.CurrentCulture = cultureInfo;
				Thread.CurrentThread.CurrentUICulture = cultureInfo;
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}

			ActiveLanguageChanged?.Invoke();
		}
	}

	// The language a lookup answers in when the caller names none, resolved against the very map that
	// lookup reads, so the code handed back is one that map carries.
	private static string GetActiveLanguage(Dictionary<string, Translation> translations)
	{
		var lang = _requestedLanguage;

		if (translations.ContainsKey(lang))
			return lang;

		if (translations.ContainsKey(EnCode))
			return EnCode;

		return translations.Count > 0 ? translations.Keys.First() : lang;
	}

	/// <summary>
	/// Try update <see cref="ActiveLanguage"/>.
	/// </summary>
	public static void TryUpdateActiveLanguage()
	{
		var currCulture = CultureInfo.CurrentCulture.Name;

		if (currCulture.IsEmpty() || !currCulture.Contains('-'))
			return;

		currCulture = currCulture.SplitBySep("-").First().ToLowerInvariant();

		if (_translations.ContainsKey(currCulture))
			ActiveLanguage = currCulture;
	}

	/// <summary>
	/// Get current culture info.
	/// </summary>
	public static CultureInfo CurrentCulture
		=> CultureInfo.GetCultureInfo(CultureCode);

	/// <summary>
	/// Get localized string.
	/// </summary>
	/// <param name="resourceId">Resource unique key.</param>
	/// <param name="language">Language.</param>
	/// <returns>Localized string.</returns>
	public static string GetString(string resourceId, string language = null)
	{
		var translations = _translations;

		if (!translations.TryGetValue(language.IsEmpty() ? GetActiveLanguage(translations) : language, out var translation))
		{
			RaiseMissing(resourceId, false);
			return resourceId;
		}

		var result = translation.GetTextById(resourceId);
		if (result != null)
			return result;

		RaiseMissing(resourceId, false);
		return resourceId;
	}

	/// <summary>
	/// Get localized string in <paramref name="to"/> language.
	/// </summary>
	/// <param name="text">Text.</param>
	/// <param name="from">Language of the <paramref name="text"/>.</param>
	/// <param name="to">Destination language.</param>
	/// <returns>Localized string.</returns>
	public static string Translate(this string text, string from = EnCode, string to = null)
	{
		var translations = _translations;

		if (!translations.TryGetValue(from, out var fromTranslation) ||
			!translations.TryGetValue(to.IsEmpty() ? GetActiveLanguage(translations) : to, out var toTranslation))
		{
			RaiseMissing(text, true);
			return text;
		}
		else if (fromTranslation == toTranslation)
			return text;

		var id = fromTranslation.GetIdByText(text);
		if (id.IsEmpty())
		{
			RaiseMissing(text, true);
			return text;
		}

		var result = toTranslation.GetTextById(id);
		if (result.IsEmpty())
		{
			RaiseMissing(text, true);
			return text;
		}

		return result;
	}

	private static void RaiseMissing(string text, bool isText)
		=> Missing?.Invoke(text, isText);
}