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
	private class Translation
	{
		private readonly Dictionary<string, string> _stringsById = new();
		private readonly Dictionary<string, string> _idsByString = new();

		public void Add(string id, string text)
		{
			_stringsById.Add(id, text);
			_idsByString[text] = id;
		}

		public string GetTextById(string id) => _stringsById.TryGetValue(id, out var text) ? text : null;
		public string GetIdByText(string text) => _idsByString.TryGetValue(text, out var id) ? id : null;
	}

	private static readonly List<Translation> _translations = new();
	private static readonly Dictionary<string, int> _langIds = new(StringComparer.InvariantCultureIgnoreCase);

	static LocalizedStrings()
	{
		static void addLanguage(Assembly asm, string langCode)
		{
			if (asm is null)
				throw new ArgumentNullException(nameof(asm));

			var stream = asm.GetManifestResourceStream($"{asm.GetName().Name}.{_stringsFileName}");

			if (stream is null)
				return;

			using var reader = new StreamReader(stream);
			var strings = reader.ReadToEnd().DeserializeObject<IDictionary<string, string>>();

			var translation = new Translation();

			foreach (var pair in strings)
				translation.Add(pair.Key, pair.Value);

			_langIds.Add(langCode, _langIds.Count);
			_translations.Add(translation);
		}

		try
		{
			var mainAsm = typeof(LocalizedStrings).Assembly;
			addLanguage(mainAsm, EnCode);

			foreach (var resFile in Directory.GetFiles(global::System.IO.Path.GetDirectoryName(mainAsm.Location), "StockSharp.Localization.*.dll"))
			{
				try
				{
					var lang = global::System.IO.Path.GetFileNameWithoutExtension(resFile).Remove("StockSharp.Localization.", true);

					if (lang.Length != 2)
						continue;

					var asm = global::System.Reflection.Assembly.LoadFrom(resFile);
					addLanguage(asm, lang);
				}
				catch (Exception ex)
				{
					Trace.WriteLine(ex);
				}
			}

			var currCulture = CultureInfo.CurrentCulture.Name;

			if (!currCulture.IsEmpty() && currCulture.Contains('-'))
			{
				currCulture = currCulture.SplitBySep("-").First().ToLowerInvariant();

				if (_langIds.ContainsKey(currCulture))
					ActiveLanguage = currCulture;
			}
		}
		catch (Exception ex)
		{
			Trace.WriteLine(InitError = ex);
		}
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
			if (!_langIds.ContainsKey(value))
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
	public static string Translate(this string text, string from = null, string to = null)
	{
		var langIdFrom = GetLangCode(from.IsEmpty(EnCode));
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