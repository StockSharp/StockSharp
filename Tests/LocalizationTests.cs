namespace StockSharp.Tests;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using StockSharp.Localization;

[TestClass]
// The registry of languages, the active one, the generated string cache and the culture it puts on
// this thread are all process-wide, and the suite otherwise runs methods in parallel.
[DoNotParallelize]
public class LocalizationTests : BaseTestClass
{
	private const string _repoRoot = "../../../../";
	private const string _enStringsFile = _repoRoot + "Localization/strings.json";
	private const string _langsDir = _repoRoot + "Localization.Langs";
	private const string _stringsFileName = "strings.json";

	private string _activeLanguage;
	private string[] _langCodes;
	private CultureInfo _culture;
	private CultureInfo _uiCulture;

	private static IDictionary<string, string> LoadStrings(string path)
		=> File.ReadAllText(path).DeserializeObject<IDictionary<string, string>>();

	// Every folder under Localization.Langs that actually carries a resource; "all" only aggregates the others.
	private static IEnumerable<string> GetLangDirs()
		=> Directory.GetDirectories(_langsDir).Where(d => File.Exists(Path.Combine(d, _stringsFileName)));

	// Composite-format holes the way string.Format reads them: "{{" and "}}" are escapes, and a hole's name is
	// what precedes its alignment or format specifier. Names, not positions - reordering in a translation is fine.
	private static List<string> GetHoles(string text)
	{
		var holes = new List<string>();

		for (var i = 0; i < text.Length; i++)
		{
			var c = text[i];

			if (c == '{')
			{
				if (i + 1 < text.Length && text[i + 1] == '{')
				{
					i++;
					continue;
				}

				var end = text.IndexOf('}', i + 1);

				if (end < 0)
					throw new FormatException($"unclosed '{{' in '{text}'");

				holes.Add(text[(i + 1)..end].Split(',', ':')[0]);
				i = end;
			}
			else if (c == '}')
			{
				if (i + 1 < text.Length && text[i + 1] == '}')
				{
					i++;
					continue;
				}

				throw new FormatException($"unescaped '}}' in '{text}'");
			}
		}

		holes.Sort(StringComparer.Ordinal);
		return holes;
	}

	// The two cultures a freshly started thread sees. A new thread is the deterministic probe: it inherits no
	// culture from the caller, and a bounded join keeps the test from hanging if it never runs.
	private static (string culture, string uiCulture) ReadCulturesOnNewThread()
	{
		var culture = string.Empty;
		var uiCulture = string.Empty;

		var thread = new Thread(() =>
		{
			culture = CultureInfo.CurrentCulture.Name;
			uiCulture = CultureInfo.CurrentUICulture.Name;
		})
		{
			IsBackground = true,
		};

		thread.Start();

		IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "the probe thread never reported its culture");

		return (culture, uiCulture);
	}

	// A test that fails before its own finally runs would otherwise leave its probe language registered
	// and active, and the next test would read that language's words. The state is taken back here so
	// no test depends on another one having cleaned up after itself.
	[TestInitialize]
	public void SaveLanguageState()
	{
		_activeLanguage = LocalizedStrings.ActiveLanguage;
		_langCodes = [.. LocalizedStrings.LangCodes];
		_culture = CultureInfo.CurrentCulture;
		_uiCulture = CultureInfo.CurrentUICulture;
	}

	[TestCleanup]
	public void RestoreLanguageState()
	{
		foreach (var code in LocalizedStrings.LangCodes.Except(_langCodes).ToArray())
			LocalizedStrings.RemoveLanguage(code);

		LocalizedStrings.ActiveLanguage = _activeLanguage;
		LocalizedStrings.ResetCache();

		CultureInfo.CurrentCulture = _culture;
		CultureInfo.CurrentUICulture = _uiCulture;
	}

	// Switching the language must drop the generated cache, or a property keeps answering in the language the
	// caller has just left.
	[TestMethod]
	[DoNotParallelize]
	public void ActiveLanguage_Switch_RefreshesTheCachedProperty()
	{
		const string code = "q1";

		var previous = LocalizedStrings.ActiveLanguage;

		try
		{
			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>
			{
				{ LocalizedStrings.LanguageKey, "L-ONE" },
				{ LocalizedStrings.CultureCodeKey, "en-US" },
			});

			LocalizedStrings.ActiveLanguage = code;
			AreEqual("L-ONE", LocalizedStrings.Language);

			LocalizedStrings.ActiveLanguage = previous;

			AreNotEqual("L-ONE", LocalizedStrings.Language);
			AreEqual(LocalizedStrings.GetString(LocalizedStrings.LanguageKey, previous), LocalizedStrings.Language);
		}
		finally
		{
			LocalizedStrings.ActiveLanguage = previous;
			LocalizedStrings.RemoveLanguage(code);
			LocalizedStrings.ResetCache();
		}
	}

	// The language is an application-wide choice, not a per-thread one: a thread started after the switch - a pool
	// worker, a background job, a UI thread - must format numbers and dates in the language just chosen.
	[TestMethod]
	[DoNotParallelize]
	public void ActiveLanguage_Switch_ReachesThreadsStartedAfterwards()
	{
		const string code = "q1";

		// The culture a thread gets when nothing was chosen for it; the target must differ from it, or the
		// assertions below would hold even without the switch.
		var ambient = ReadCulturesOnNewThread().culture;
		var target = ambient.EqualsIgnoreCase("de-DE") ? "fr-FR" : "de-DE";

		var previous = LocalizedStrings.ActiveLanguage;

		try
		{
			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>
			{
				{ LocalizedStrings.LanguageKey, "L-ONE" },
				{ LocalizedStrings.CultureCodeKey, target },
			});

			LocalizedStrings.ActiveLanguage = code;

			AreEqual(target, LocalizedStrings.CurrentCulture.Name);
			AreEqual(target, CultureInfo.CurrentCulture.Name, "the calling thread kept its previous culture");

			var (culture, uiCulture) = ReadCulturesOnNewThread();

			AreEqual(target, culture, "a thread started after the switch formats in another language");
			AreEqual(target, uiCulture, "a thread started after the switch looks resources up in another language");
		}
		finally
		{
			LocalizedStrings.ActiveLanguage = previous;
			LocalizedStrings.RemoveLanguage(code);
			LocalizedStrings.ResetCache();
		}
	}

	/// <summary>
	/// The change notification is how a window that is already on screen learns to relabel itself, so a
	/// handler is entitled to find the new language already in force when it runs - reading a caption
	/// there must give the new words, not the ones being left. It is equally entitled not to be woken
	/// for nothing: re-selecting the language already in use, in any letter case, and naming a language
	/// that was never registered both leave the choice alone and raise nothing.
	/// </summary>
	[TestMethod]
	[DoNotParallelize]
	public void ActiveLanguageChanged_FiresOnceWithTheNewLanguageAlreadyInForce()
	{
		const string code = "q1";

		var seen = new List<string>();

		void onChanged() => seen.Add(LocalizedStrings.Language);

		LocalizedStrings.ActiveLanguageChanged += onChanged;

		try
		{
			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>
			{
				{ LocalizedStrings.LanguageKey, "L-ONE" },
				{ LocalizedStrings.CultureCodeKey, "en-US" },
			});

			LocalizedStrings.ActiveLanguage = code;

			HasCount(1, seen);
			AreEqual("L-ONE", seen[0], "the handler ran before the switch reached the cached properties");

			// The same language again, and the same language spelled differently.
			LocalizedStrings.ActiveLanguage = code;
			LocalizedStrings.ActiveLanguage = code.ToUpperInvariant();

			HasCount(1, seen);
			AreEqual(code, LocalizedStrings.ActiveLanguage);

			// A language nobody registered is not a language to switch to.
			LocalizedStrings.ActiveLanguage = "q9";

			HasCount(1, seen);
			AreEqual(code, LocalizedStrings.ActiveLanguage, "the active language was replaced by an unregistered one");
		}
		finally
		{
			LocalizedStrings.ActiveLanguageChanged -= onChanged;
		}
	}

	/// <summary>
	/// An application that was never told which language to use follows the machine - a user whose
	/// system is set to a language that ships gets it without touching a setting. It follows the
	/// machine only that far: a system language nobody translated, or one the machine states without a
	/// region, leaves the current choice standing rather than pointing the registry at a language that
	/// carries no words, which shows resource keys instead of text everywhere at once.
	/// </summary>
	[TestMethod]
	[DoNotParallelize]
	public void TryUpdateActiveLanguage_FollowsTheMachineOnlyToALanguageThatShips()
	{
		// Two codes that are not StockSharp languages, so this test neither depends on nor disturbs a
		// shipped one; both are real cultures, which is what the machine reports.
		const string shipped = "mt";
		const string unshipped = "lb";

		IsFalse(LocalizedStrings.LangCodes.Contains(shipped), $"'{shipped}' is now a shipped language; pick another probe");
		IsFalse(LocalizedStrings.LangCodes.Contains(unshipped), $"'{unshipped}' is now a shipped language; pick another probe");

		LocalizedStrings.AddLanguage(shipped, new Dictionary<string, string>
		{
			{ LocalizedStrings.LanguageKey, "L-ONE" },
			{ LocalizedStrings.CultureCodeKey, "mt-MT" },
		});

		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("mt-MT");
		LocalizedStrings.TryUpdateActiveLanguage();

		AreEqual(shipped, LocalizedStrings.ActiveLanguage, "the machine language was not picked up");
		AreEqual("L-ONE", LocalizedStrings.Language);

		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo($"{unshipped}-LU");
		LocalizedStrings.TryUpdateActiveLanguage();

		AreEqual(shipped, LocalizedStrings.ActiveLanguage, "an untranslated machine language replaced the active one");

		// A machine that names no region names no language either.
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		LocalizedStrings.TryUpdateActiveLanguage();

		AreEqual(shipped, LocalizedStrings.ActiveLanguage, "a culture without a region replaced the active language");
		AreEqual("L-ONE", LocalizedStrings.Language);
	}

	// Removing the language a caller is reading must take its words with it: a generated property may not keep
	// answering with text from a language that is no longer registered, and must agree with GetString for the
	// same key. Re-registering the code with different words must reach the property too.
	[TestMethod]
	[DoNotParallelize]
	public void RemoveLanguage_ActiveOne_DropsTheTranslationItCarried()
	{
		const string code = "q1";

		var previous = LocalizedStrings.ActiveLanguage;
		var changeCount = 0;

		void onChanged() => changeCount++;

		LocalizedStrings.ActiveLanguageChanged += onChanged;

		try
		{
			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>
			{
				{ LocalizedStrings.LanguageKey, "L-ONE" },
				{ LocalizedStrings.CultureCodeKey, "en-US" },
			});

			LocalizedStrings.ActiveLanguage = code;
			AreEqual("L-ONE", LocalizedStrings.Language);

			IsTrue(LocalizedStrings.RemoveLanguage(code));

			AreNotEqual("L-ONE", LocalizedStrings.Language, "a removed language still answers through the cached property");
			AreEqual(LocalizedStrings.GetString(LocalizedStrings.LanguageKey), LocalizedStrings.Language);
			changeCount = 0;

			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>
			{
				{ LocalizedStrings.LanguageKey, "L-TWO" },
				{ LocalizedStrings.CultureCodeKey, "en-US" },
			});

			AreEqual("L-TWO", LocalizedStrings.GetString(LocalizedStrings.LanguageKey));
			AreEqual("L-TWO", LocalizedStrings.Language);
			AreEqual(1, changeCount, "re-registering the requested language must notify active-language listeners exactly once");
		}
		finally
		{
			LocalizedStrings.ActiveLanguageChanged -= onChanged;
			LocalizedStrings.ActiveLanguage = previous;
			LocalizedStrings.RemoveLanguage(code);
			LocalizedStrings.ResetCache();
		}
	}

	// Whatever removal decides to do with the code that is active - drop it for a fallback, or refuse - it may not
	// leave the registry pointing at a language it no longer carries: every lookup then answers with the bare key,
	// so the whole application shows resource ids instead of words.
	[TestMethod]
	[DoNotParallelize]
	public void RemoveLanguage_ActiveOne_LeavesARegisteredLanguageActive()
	{
		const string code = "q1";

		var previous = LocalizedStrings.ActiveLanguage;

		try
		{
			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>
			{
				{ LocalizedStrings.LanguageKey, "L-ONE" },
				{ LocalizedStrings.CultureCodeKey, "en-US" },
			});

			LocalizedStrings.ActiveLanguage = code;
			LocalizedStrings.RemoveLanguage(code);

			var active = LocalizedStrings.ActiveLanguage;

			IsTrue(LocalizedStrings.LangCodes.Contains(active), $"active language '{active}' is not registered");
			// CultureCode rather than Language: its English text is not the key itself, so a key coming back is unambiguous.
			AreNotEqual(LocalizedStrings.CultureCodeKey, LocalizedStrings.GetString(LocalizedStrings.CultureCodeKey), "lookups answer with the resource key");
		}
		finally
		{
			LocalizedStrings.ActiveLanguage = previous;
			LocalizedStrings.RemoveLanguage(code);
			LocalizedStrings.ResetCache();
		}
	}

	// Removing one language renumbers the rest, so every other language - the ones registered before it as well
	// as after - must keep answering with its own text.
	[TestMethod]
	[DoNotParallelize]
	public void RemoveLanguage_MiddleOne_LeavesTheOthersAddressable()
	{
		const string key = "LocalizationTests_Key";

		var en = LocalizedStrings.GetString(LocalizedStrings.LanguageKey, LocalizedStrings.EnCode);

		try
		{
			LocalizedStrings.AddLanguage("q1", new Dictionary<string, string> { { key, "V1" } });
			LocalizedStrings.AddLanguage("q2", new Dictionary<string, string> { { key, "V2" } });
			LocalizedStrings.AddLanguage("q3", new Dictionary<string, string> { { key, "V3" } });

			AreEqual("V1", LocalizedStrings.GetString(key, "q1"));
			AreEqual("V2", LocalizedStrings.GetString(key, "q2"));
			AreEqual("V3", LocalizedStrings.GetString(key, "q3"));

			IsTrue(LocalizedStrings.RemoveLanguage("q2"));

			AreEqual("V1", LocalizedStrings.GetString(key, "q1"));
			AreEqual("V3", LocalizedStrings.GetString(key, "q3"));
			AreEqual(en, LocalizedStrings.GetString(LocalizedStrings.LanguageKey, LocalizedStrings.EnCode));

			// An unregistered language carries no text at all, so the key comes back unchanged.
			AreEqual(key, LocalizedStrings.GetString(key, "q2"));

			// Now the first of the three, which shifts every later index again.
			IsTrue(LocalizedStrings.RemoveLanguage("q1"));

			AreEqual("V3", LocalizedStrings.GetString(key, "q3"));
			AreEqual(en, LocalizedStrings.GetString(LocalizedStrings.LanguageKey, LocalizedStrings.EnCode));
		}
		finally
		{
			LocalizedStrings.RemoveLanguage("q1");
			LocalizedStrings.RemoveLanguage("q2");
			LocalizedStrings.RemoveLanguage("q3");
		}
	}

	// The code is the whole addressing scheme: an empty one is a programming error, removing a code that was
	// never added reports false instead of disturbing the registry, and adding a code twice must not leave the
	// registry half-written.
	[TestMethod]
	[DoNotParallelize]
	public void RemoveLanguage_RejectsEmptyCodeAndReportsUnknownOne()
	{
		Throws<ArgumentNullException>(() => LocalizedStrings.RemoveLanguage(null));
		Throws<ArgumentNullException>(() => LocalizedStrings.RemoveLanguage(string.Empty));

		const string code = "q9";
		const string key = "LocalizationTests_Key";

		IsFalse(LocalizedStrings.LangCodes.Contains(code));
		IsFalse(LocalizedStrings.RemoveLanguage(code));
		IsTrue(LocalizedStrings.LangCodes.Contains(LocalizedStrings.EnCode));

		LocalizedStrings.AddLanguage(code, new Dictionary<string, string> { { key, "first" } });

		try
		{
			Throws<ArgumentException>(() => LocalizedStrings.AddLanguage(code, new Dictionary<string, string> { { key, "second" } }));
			AreEqual("first", LocalizedStrings.GetString(key, code));
		}
		finally
		{
			LocalizedStrings.RemoveLanguage(code);
		}
	}

	// A key nobody has translated comes back as itself, so the caller sees the key on screen rather than an empty
	// label, and is reported exactly once so the report names the key to add. A key that is there is not reported.
	[TestMethod]
	[DoNotParallelize]
	public void GetString_MissingKeyOrLanguage_ReturnsTheKeyAndReportsItOnce()
	{
		const string key = "LocalizationTests_NoSuchKey";

		var reported = new List<string>();

		void onMissing(string text, bool isText)
		{
			// A resource id is reported with isText false; the by-text report is a different signal entirely.
			IsFalse(isText);
			reported.Add(text);
		}

		LocalizedStrings.Missing += onMissing;

		try
		{
			AreEqual(key, LocalizedStrings.GetString(key));
			HasCount(1, reported);
			AreEqual(key, reported[0]);

			reported.Clear();

			AreEqual(key, LocalizedStrings.GetString(key, "q9"));
			HasCount(1, reported);
			AreEqual(key, reported[0]);

			reported.Clear();

			IsNotEmpty(LocalizedStrings.GetString(LocalizedStrings.LanguageKey, LocalizedStrings.EnCode));
			HasCount(0, reported);
		}
		finally
		{
			LocalizedStrings.Missing -= onMissing;
		}
	}

	// Translate answers with the destination text when it can and otherwise hands back exactly what it was given,
	// reported once - never an empty string, and never silently. Same language in and out is an identity.
	[TestMethod]
	[DoNotParallelize]
	public void Translate_MissingTextOrLanguage_ReturnsTheInputAndReportsItOnce()
	{
		var reported = new List<string>();

		void onMissing(string text, bool isText)
		{
			IsTrue(isText);
			reported.Add(text);
		}

		try
		{
			LocalizedStrings.AddLanguage("qa", new Dictionary<string, string> { { "K1", "alpha" }, { "K2", "delta" } });
			LocalizedStrings.AddLanguage("qb", new Dictionary<string, string> { { "K1", "beta" } });

			LocalizedStrings.Missing += onMissing;

			AreEqual("beta", "alpha".Translate("qa", "qb"));
			HasCount(0, reported);

			// Text that is in no resource at all.
			AreEqual("gamma", "gamma".Translate("qa", "qb"));
			HasCount(1, reported);
			AreEqual("gamma", reported[0]);
			reported.Clear();

			// Text whose key the destination language does not carry.
			AreEqual("delta", "delta".Translate("qa", "qb"));
			HasCount(1, reported);
			AreEqual("delta", reported[0]);
			reported.Clear();

			// An unregistered destination language.
			AreEqual("alpha", "alpha".Translate("qa", "q9"));
			HasCount(1, reported);
			AreEqual("alpha", reported[0]);
			reported.Clear();

			AreEqual("gamma", "gamma".Translate("qa", "qa"));
			HasCount(0, reported);
		}
		finally
		{
			LocalizedStrings.Missing -= onMissing;
			LocalizedStrings.RemoveLanguage("qa");
			LocalizedStrings.RemoveLanguage("qb");
		}
	}

	// Several keys can share one English text, so a by-text lookup has no single right answer and the API does
	// not settle which key wins. What is not in doubt: it must still translate rather than fall through to the
	// source text, and the by-key lookup stays exact for both keys.
	[TestMethod]
	[DoNotParallelize]
	public void Translate_AmbiguousText_StillResolvesToARegisteredTranslation()
	{
		try
		{
			LocalizedStrings.AddLanguage("qa", new Dictionary<string, string> { { "K1", "Same" }, { "K2", "Same" } });
			LocalizedStrings.AddLanguage("qb", new Dictionary<string, string> { { "K1", "AAA" }, { "K2", "BBB" } });

			var translated = "Same".Translate("qa", "qb");

			IsTrue(translated == "AAA" || translated == "BBB", $"ambiguous text translated to '{translated}'");

			AreEqual("AAA", LocalizedStrings.GetString("K1", "qb"));
			AreEqual("BBB", LocalizedStrings.GetString("K2", "qb"));
		}
		finally
		{
			LocalizedStrings.RemoveLanguage("qa");
			LocalizedStrings.RemoveLanguage("qb");
		}
	}

	/// <summary>
	/// The registry is read by every thread that puts a word on screen, and written whenever a language
	/// pack is registered or dropped, so the two meet. A reader may find a probe language present or
	/// absent - both are true at some instant - but it may never be handed another language's text under
	/// the code it asked for, nor an exception, and a language nobody touched must keep answering.
	/// </summary>
	[TestMethod]
	[DoNotParallelize]
	public void Registry_WrittenWhileRead_NeverAnswersWithAnotherLanguagesText()
	{
		const string key = "LocalizationTests_RaceKey";

		// The second is registered after the first and dropped after it: that order is what a registry
		// addressing a language by its position, rather than by its code, gets wrong.
		const string first = "z1";
		const string second = "z2";

		const string firstText = "FIRST";
		const string secondText = "SECOND";

		const int cycles = 3000;
		const int readerCount = 4;
		const int readsPerReader = 500;

		var english = LocalizedStrings.GetString(LocalizedStrings.LanguageKey, LocalizedStrings.EnCode);

		// Counted apart so that neither kind of failure can crowd the other out of the report.
		var wrong = new ConcurrentQueue<string>();
		var thrown = new ConcurrentQueue<string>();
		var reads = 0L;

		static void report(ConcurrentQueue<string> problems, string problem)
		{
			if (problems.Count < 10)
				problems.Enqueue(problem);
		}

		// The two are tied to each other so that the meeting the test is named for cannot be missed:
		// the writer does not start until every reader is in its loop, and does not stop until every
		// reader is out of it. Left to the scheduler, a writer that ran to completion first would leave
		// the readers with nothing to race and the run would prove nothing.
		using var readersReady = new CountdownEvent(readerCount);
		var readersDone = 0;

		try
		{
			var readers = Enumerable.Range(0, readerCount).Select(_ => Task.Run(() =>
			{
				readersReady.Signal();

				for (var i = 0; i < readsPerReader; i++)
				{
					try
					{
						var v1 = LocalizedStrings.GetString(key, first);

						if (v1 != firstText && v1 != key)
							report(wrong, $"'{first}' answered '{v1}'");

						var v2 = LocalizedStrings.GetString(key, second);

						if (v2 != secondText && v2 != key)
							report(wrong, $"'{second}' answered '{v2}'");

						var en = LocalizedStrings.GetString(LocalizedStrings.LanguageKey, LocalizedStrings.EnCode);

						if (en != english)
							report(wrong, $"'{LocalizedStrings.EnCode}' answered '{en}'");

						Interlocked.Increment(ref reads);
					}
					catch (Exception ex)
					{
						report(thrown, $"reader: {ex.GetType().Name}: {ex.Message}");
					}
				}

				Interlocked.Increment(ref readersDone);
			})).ToArray();

			var writer = Task.Run(() =>
			{
				try
				{
					readersReady.Wait();

					for (var i = 0; i < cycles || Volatile.Read(ref readersDone) < readerCount; i++)
					{
						LocalizedStrings.AddLanguage(first, new Dictionary<string, string> { { key, firstText } });
						LocalizedStrings.AddLanguage(second, new Dictionary<string, string> { { key, secondText } });
						LocalizedStrings.RemoveLanguage(first);
						LocalizedStrings.RemoveLanguage(second);
					}
				}
				catch (Exception ex)
				{
					report(thrown, $"writer: {ex.GetType().Name}: {ex.Message}");
				}
			});

			var all = readers.Append(writer).ToArray();

			IsTrue(Task.WaitAll(all, TimeSpan.FromMinutes(2)), "the registry never let the readers or the writer finish");
		}
		finally
		{
			LocalizedStrings.RemoveLanguage(first);
			LocalizedStrings.RemoveLanguage(second);
		}

		var wrongAnswers = wrong.ToArray();
		var exceptions = thrown.ToArray();

		// What went wrong is stated before whether enough went on, or a reader that threw on every pass
		// is reported as a run that did too little rather than as the failure it was.
		IsEmpty(wrongAnswers, wrongAnswers.JoinN());
		IsEmpty(exceptions, exceptions.JoinN());

		AreEqual((long)readerCount * readsPerReader, Interlocked.Read(ref reads),
			"every read was to land while the registry was being written, and this many did not");
	}

	/// <summary>
	/// A generated property answers from a cache that a change of language drops, and the value it caches
	/// is computed before it is stored. The answer a reader already holds may be the one it started with,
	/// but it may not be left behind in the cache the next reader takes: once the registry has settled,
	/// every property speaks the language that is now active. The missing-key report is the point inside
	/// the lookup where the registry is changed here, which is what a reader on another thread does at an
	/// arbitrary point anyway.
	/// </summary>
	[TestMethod]
	[DoNotParallelize]
	public void CachedProperty_RegistryChangedWhileRead_KeepsNoWordFromTheLanguageThatWentAway()
	{
		const string code = "z3";

		var previous = LocalizedStrings.ActiveLanguage;

		// Its English text is not the key itself, so a key coming back is unambiguous.
		var english = LocalizedStrings.GetString(LocalizedStrings.CultureCodeKey, LocalizedStrings.EnCode);
		var dropped = false;

		void onMissing(string text, bool isText)
		{
			if (dropped || text != LocalizedStrings.CultureCodeKey)
				return;

			dropped = true;
			LocalizedStrings.RemoveLanguage(code);
		}

		try
		{
			// The probe carries no culture of its own, so asking for one reports the key missing.
			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>
			{
				{ LocalizedStrings.LanguageKey, "L-ONE" },
			});

			LocalizedStrings.ActiveLanguage = code;
			LocalizedStrings.ResetCache();

			LocalizedStrings.Missing += onMissing;

			try
			{
				AreEqual(LocalizedStrings.CultureCodeKey, LocalizedStrings.CultureCode, "the probe language answered for a key it does not carry");
			}
			finally
			{
				LocalizedStrings.Missing -= onMissing;
			}

			IsTrue(dropped, "the lookup never reported the missing key, so nothing was dropped under it");
			AreEqual(english, LocalizedStrings.CultureCode, "the property kept a word computed for a language that was dropped while it was being read");
		}
		finally
		{
			LocalizedStrings.ActiveLanguage = previous;
			LocalizedStrings.RemoveLanguage(code);
			LocalizedStrings.ResetCache();
		}
	}

	// Same keys as English, no empty text, and the same substitution holes: a translation that drops, adds or
	// renames a hole either throws out of string.Format or silently loses an argument, and equal key counts say
	// nothing about that. Holes are compared by name so a translator may still reorder them.
	[TestMethod]
	public void Languages_KeepEnglishKeysAndSubstitutionHoles()
	{
		var en = LoadStrings(_enStringsFile);
		var problems = new List<string>();
		var enHoles = new Dictionary<string, string>(en.Count);

		foreach (var pair in en)
		{
			try
			{
				enHoles.Add(pair.Key, GetHoles(pair.Value).JoinComma());
			}
			catch (FormatException ex)
			{
				problems.Add($"en/{pair.Key}: {ex.Message}");
			}
		}

		foreach (var dir in GetLangDirs())
		{
			var code = Path.GetFileName(dir);
			var strings = LoadStrings(Path.Combine(dir, _stringsFileName));

			foreach (var key in en.Keys.Except(strings.Keys))
				problems.Add($"{code}: key '{key}' is missing");

			foreach (var key in strings.Keys.Except(en.Keys))
				problems.Add($"{code}: key '{key}' is not in the English resource");

			foreach (var pair in strings)
			{
				if (pair.Value.IsEmpty() || pair.Value.Trim().IsEmpty())
				{
					problems.Add($"{code}/{pair.Key}: no text");
					continue;
				}

				if (!enHoles.TryGetValue(pair.Key, out var expected))
					continue;

				string actual;

				try
				{
					actual = GetHoles(pair.Value).JoinComma();
				}
				catch (FormatException ex)
				{
					problems.Add($"{code}/{pair.Key}: {ex.Message}");
					continue;
				}

				if (actual != expected)
					problems.Add($"{code}/{pair.Key}: holes [{actual}] but English has [{expected}]");
			}
		}

		IsEmpty(problems, problems.Take(20).JoinN());
	}

	// The loader scans for StockSharp.Localization.*.dll beside itself and keeps only a two-letter suffix, and
	// nothing but the aggregator pulls a language project into a build. A folder that breaks either rule ships a
	// language that is silently never registered.
	[TestMethod]
	public void LanguageProjects_AreShapedForSatelliteDiscovery()
	{
		var aggregator = File.ReadAllText(Path.Combine(_langsDir, "all", "Localization.all.csproj"));
		var problems = new List<string>();

		foreach (var dir in GetLangDirs())
		{
			var code = Path.GetFileName(dir);

			if (code.Length != 2 || !code.All(char.IsAsciiLetterLower))
				problems.Add($"{code}: not a two-letter lower-case language code");

			var proj = $"Localization.{code}.csproj";

			if (!File.Exists(Path.Combine(dir, proj)))
				problems.Add($"{code}: {proj} is missing");

			if (!aggregator.Contains($@"..\{code}\{proj}"))
				problems.Add($"{code}: not referenced by Localization.all");
		}

		foreach (Match m in Regex.Matches(aggregator, @"\.\.\\(?<code>[^\\""]+)\\Localization\."))
		{
			var code = m.Groups["code"].Value;

			if (!File.Exists(Path.Combine(_langsDir, code, _stringsFileName)))
				problems.Add($"{code}: referenced by Localization.all but carries no resource");
		}

		IsEmpty(problems, problems.JoinN());
	}

	// The loader asks every assembly for "<assembly name>.strings.json" and drops the language without a word if
	// the resource is named anything else or was never embedded. Pin the name, that reading it the way a satellite
	// is read registers a working language, and that what shipped is what is on disk.
	[TestMethod]
	[DoNotParallelize]
	public void MainAssembly_EmbedsTheResourceUnderTheNameTheLoaderAsksFor()
	{
		IsNull(LocalizedStrings.InitError, "localization failed to initialize");

		var asm = typeof(LocalizedStrings).Assembly;
		var resourceName = $"{asm.GetName().Name}.{_stringsFileName}";

		Contains(asm.GetManifestResourceNames(), resourceName);

		const string code = "q0";

		try
		{
			var stream = asm.GetManifestResourceStream(resourceName);

			IsNotNull(stream);

			LocalizedStrings.AddLanguage(code, stream);
			IsTrue(LocalizedStrings.LangCodes.Contains(code));

			foreach (var pair in LoadStrings(_enStringsFile))
			{
				AreEqual(pair.Value, LocalizedStrings.GetString(pair.Key, code));
				AreEqual(pair.Value, LocalizedStrings.GetString(pair.Key, LocalizedStrings.EnCode));
			}
		}
		finally
		{
			LocalizedStrings.RemoveLanguage(code);
		}
	}

	// The generator is the only thing that keeps the members and strings.json in step, and nothing in the product
	// reads every key - so a generator that quietly drops or invents one still compiles. One "<key>Key" constant
	// carrying the key itself, one same-named string property, per resource key, and nothing left over.
	[TestMethod]
	public void GeneratedSurface_MatchesTheStringsResource()
	{
		var en = LoadStrings(_enStringsFile);
		var type = typeof(LocalizedStrings);

		var emitted = type
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(f => f.IsLiteral && f.FieldType == typeof(string))
			.Select(f => (name: f.Name, value: (string)f.GetRawConstantValue()))
			.Where(f => f.name == f.value + "Key")
			.Select(f => f.value)
			.ToArray();

		IsEmpty(en.Keys.Except(emitted).ToArray(), "resource keys with no generated constant");
		IsEmpty(emitted.Except(en.Keys).ToArray(), "generated constants with no resource key");

		var problems = new List<string>();

		foreach (var key in en.Keys)
		{
			var prop = type.GetProperty(key, BindingFlags.Public | BindingFlags.Static);

			if (prop is null)
				problems.Add($"{key}: no public static property");
			else if (prop.PropertyType != typeof(string))
				problems.Add($"{key}: property is {prop.PropertyType.Name}, not string");
		}

		IsEmpty(problems, problems.Take(20).JoinN());

		// ResetCache is the only way a caller drops the cached properties after a language change.
		IsNotNull(type.GetMethod(nameof(LocalizedStrings.ResetCache), BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null));
	}
}
