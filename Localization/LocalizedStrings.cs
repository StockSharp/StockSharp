namespace StockSharp.Localization;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Ecng.Collections;
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
	}

	private class Translation
	{
		private readonly Dictionary<string, string> _stringsById = [];
		private readonly Dictionary<string, string> _idsByString = [];

		public void Add(string id, string text)
		{
			_stringsById.Add(id, text);
			_idsByString[text] = id;
		}

		public string GetTextById(string id) => _stringsById.TryGetValue(id, out var text) ? text : null;
		public string GetIdByText(string text) => _idsByString.TryGetValue(text, out var id) ? id : null;
	}

	private static readonly List<Translation> _translations = [];
	private static readonly Dictionary<string, int> _langIds = new(StringComparer.InvariantCultureIgnoreCase);

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

		var translation = new Translation();

		foreach (var pair in strings)
			translation.Add(pair.Key, pair.Value);

		_langIds.Add(langCode, _langIds.Count);
		_translations.Add(translation);
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

		if (!_langIds.TryGetAndRemove(langCode, out var langId))
			return false;

		_translations.RemoveAt(langId);

		foreach (var p in _langIds.ToArray())
		{
			if (p.Value < langId)
				continue;

			_langIds[p.Key] = p.Value - 1;
		}

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
	public static IEnumerable<string> LangCodes => _langIds.Keys;

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

	private static string _activeLanguage = EnCode;

	/// <summary>
	/// Current language.
	/// </summary>
	public static string ActiveLanguage
	{
		get => _activeLanguage;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			if (ActiveLanguage.EqualsIgnoreCase(value) || !_langIds.ContainsKey(value))
				return;

			_activeLanguage = value;
			ResetCache();

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

	/// <summary>
	/// Try update <see cref="ActiveLanguage"/>.
	/// </summary>
	public static void TryUpdateActiveLanguage()
	{
		var currCulture = CultureInfo.CurrentCulture.Name;

		if (currCulture.IsEmpty() || !currCulture.Contains('-'))
			return;

		currCulture = currCulture.SplitBySep("-").First().ToLowerInvariant();

		if (_langIds.ContainsKey(currCulture))
			ActiveLanguage = currCulture;
	}

	/// <summary>
	/// Get current culture info.
	/// </summary>
	public static CultureInfo CurrentCulture
		=> CultureInfo.GetCultureInfo(CultureCode);

	private static int GetLangCode(string lang)
		=> _langIds.TryGetValue(lang, out var langCode) ? langCode : -1;

	/// <summary>
	/// Get localized string.
	/// </summary>
	/// <param name="resourceId">Resource unique key.</param>
	/// <param name="language">Language.</param>
	/// <returns>Localized string.</returns>
	public static string GetString(string resourceId, string language = null)
	{
		var langId = GetLangCode(language.IsEmpty(ActiveLanguage));
		if (langId < 0)
		{
			Missing?.Invoke(resourceId, false);
			return resourceId;
		}

		var result = _translations[langId].GetTextById(resourceId);
		if (result != null)
			return result;

		Missing?.Invoke(resourceId, false);
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
		var langIdFrom = GetLangCode(from);
		var langIdTo = GetLangCode(to.IsEmpty(ActiveLanguage));

		if (langIdFrom < 0 || langIdTo < 0)
		{
			Missing?.Invoke(text, true);
			return text;
		}
		else if (langIdFrom == langIdTo)
			return text;

		var id = _translations[langIdFrom].GetIdByText(text);
		if (id.IsEmpty())
		{
			Missing?.Invoke(text, true);
			return text;
		}

		var result = _translations[langIdTo].GetTextById(id);
		if (result.IsEmpty())
		{
			Missing?.Invoke(text, true);
			return text;
		}

		return result;
	}
}