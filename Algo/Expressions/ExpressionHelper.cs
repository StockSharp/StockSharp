namespace StockSharp.Algo.Expressions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Security;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// Extension class for <see cref="ExpressionIndexSecurity"/>.
	/// </summary>
	[CLSCompliant(false)]
	public static class ExpressionHelper
	{
		private const string _secIdPattern = @"(#*)(@*)(#*)(\w*\.*)(\**)(\w+(\/*)\w+)@\w+";

		private static readonly Regex _secIdRegex = new Regex($@"(?<secId>{_secIdPattern})");
		private static readonly Regex _nameRegex = new Regex(@"(?<name>(\w+))");
		private static readonly Regex _decodeRegex = new Regex($@"\[{_secIdPattern}\]");

		/// <summary>
		/// To get all <see cref="Security.Id"/> from mathematic formula.
		/// </summary>
		/// <param name="expression">Mathematical formula.</param>
		/// <returns>IDs securities.</returns>
		public static IEnumerable<string> GetSecurityIds(string expression)
		{
			return
				from Match match in _secIdRegex.Matches(expression)
				where match.Success
				select match.Groups["secId"].Value;
		}

		private static IEnumerable<Group> GetVariableNames(string expression)
		{
			return
				from Match match in _nameRegex.Matches(expression)
				where match.Success
				select match.Groups["name"];
		}

		/// <summary>
		/// To screen off mathematic formula from instruments identifiers <see cref="Security.Id"/>.
		/// </summary>
		/// <param name="expression">The source text.</param>
		/// <returns>The screened text.</returns>
		public static string Encode(string expression)
		{
			foreach (var secId in GetSecurityIds(expression).Distinct(StringComparer.InvariantCultureIgnoreCase))
			{
				expression = expression.Replace(secId, "[{0}]".Put(secId));
			}

			return expression;
		}

		/// <summary>
		/// To screen on mathematic formula with instruments identifiers <see cref="Security.Id"/>.
		/// </summary>
		/// <param name="expression">The source text.</param>
		/// <returns>The unscreened text.</returns>
		public static string Decode(string expression)
		{
			foreach (var match in _decodeRegex.Matches(expression).Cast<Match>().OrderByDescending(m => m.Index))
			{
				expression = expression.Remove(match.Index, match.Length);
				expression = expression.Insert(match.Index, match.Value.Substring(1, match.Value.Length - 2));
				//expression = expression.Replace(match.Groups[0].Value, match.Groups[1].Value);
			}

			return expression;
		}

		/// <summary>
		/// Available functions.
		/// </summary>
		public static IEnumerable<string> Functions => _funcReplaces.CachedKeys;

		private const string _prefix = nameof(MathHelper) + ".";
		private static readonly CachedSynchronizedDictionary<string, string> _funcReplaces = new CachedSynchronizedDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
		{
			{ "abs", _prefix + nameof(MathHelper.Abs) },
			{ "acos", _prefix + nameof(MathHelper.Acos) },
			{ "asin", _prefix + nameof(MathHelper.Asin) },
			{ "atan", _prefix + nameof(MathHelper.Atan) },
			{ "ceiling", _prefix + nameof(MathHelper.Ceiling) },
			{ "cos", _prefix + nameof(MathHelper.Cos) },
			{ "exp", _prefix + nameof(MathHelper.Exp) },
			{ "floor", _prefix + nameof(MathHelper.Floor) },
			//{ "ieeeremainder", _prefix + nameof(MathHelper.IEEERemainer) },
			{ "log", _prefix + nameof(MathHelper.Log) },
			{ "log10", _prefix + nameof(MathHelper.Log10) },
			{ "max", _prefix + nameof(MathHelper.Max) },
			{ "min", _prefix + nameof(MathHelper.Min) },
			{ "pow", _prefix + nameof(MathHelper.Pow) },
			{ "round", _prefix + nameof(MathHelper.Round) },
			{ "sign", _prefix + nameof(MathHelper.Sign) },
			{ "sin", _prefix + nameof(MathHelper.Sin) },
			{ "sqrt", _prefix + nameof(MathHelper.Sqrt) },
			{ "tan", _prefix + nameof(MathHelper.Tan) },
			{ "truncate", _prefix + nameof(MathHelper.Truncate) },
		};

		private class ErrorExpressionFormula : ExpressionFormula
		{
			public ErrorExpressionFormula(string error)
				: base(error)
			{
			}

			public override decimal Calculate(decimal[] prices)
			{
				throw new NotSupportedException(Error);
			}
		}

		internal static ExpressionFormula CreateError(string errorText)
		{
			return new ErrorExpressionFormula(errorText);
		}

		private static string ReplaceFuncs(string text)
		{
			var dict = new Dictionary<string, string>();

			foreach (var pair in _funcReplaces.CachedPairs)
			{
				var what = pair.Key + "(";

				if (!text.ContainsIgnoreCase(what))
					continue;

				var rnd = CryptoHelper.GenerateSalt(16).Base64();

				dict.Add(rnd, pair.Value + "(");
				text = text.ReplaceIgnoreCase(what, rnd);
			}

			foreach (var pair in dict)
			{
				text = text.ReplaceIgnoreCase(pair.Key, pair.Value);
			}

			return text;
		}

		private static string Escape(string text, bool useSecurityIds, out IEnumerable<string> identifiers)
		{
			if (text.IsEmptyOrWhiteSpace())
				throw new ArgumentNullException(nameof(text));

			if (useSecurityIds)
			{
				text = Decode(text.ToUpperInvariant());
				identifiers = GetSecurityIds(text).Distinct().ToArray();

				var i = 0;
				foreach (var id in identifiers)
				{
					text = text.ReplaceIgnoreCase(id, $"values[{i}]");
					i++;
				}

				if (i == 0)
					throw new InvalidOperationException(LocalizedStrings.NoSecIdsFound.Put(text));

				return ReplaceFuncs(text);
			}
			else
			{
				//var textWithoutFunctions = _funcReplaces
				//	.CachedPairs
				//	.Aggregate(text, (current, pair) => current.ReplaceIgnoreCase(pair.Key, string.Empty));

				const string dotSep = "__DOT__";

				text = text.Replace(".", dotSep);

				var groups = GetVariableNames(text)
					.Where(g => !g.Value.ContainsIgnoreCase(dotSep) && !long.TryParse(g.Value, out var _) && !_funcReplaces.ContainsKey(g.Value))
					.ToArray();

				var dict = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

				foreach (var g in groups.OrderByDescending(g => g.Index))
				{
					var i = dict.TryGetValue2(g.Value);

					if (i == null)
					{
						i = dict.Count;
						dict.Add(g.Value, i.Value);
					}
					
					text = text.Remove(g.Index, g.Length).Insert(g.Index, $"values[{i}]");
				}

				identifiers = dict.Keys.ToArray();

				text = text.Replace(dotSep, ".");

				return ReplaceFuncs(text);
			}
		}

		private const string _template = @"using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Expressions;

class TempExpressionFormula : ExpressionFormula
{
	public TempExpressionFormula(string expression, IEnumerable<string> securityIds)
		: base(expression, securityIds)
	{
	}

	public override decimal Calculate(decimal[] values)
	{
		return __insert_code;
	}
}";

		/// <summary>
		/// Compile mathematical formula.
		/// </summary>
		/// <param name="service">Compiler service.</param>
		/// <param name="expression">Text expression.</param>
		/// <param name="useSecurityIds">Use security ids as variables.</param>
		/// <returns>Compiled mathematical formula.</returns>
		public static ExpressionFormula Compile(this ICompilerService service, string expression, bool useSecurityIds)
		{
			try
			{
				var code = Escape(expression, useSecurityIds, out var securityIds);
				var result = service.GetCompiler(CompilationLanguages.CSharp).Compile("IndexExpression", _template.Replace("__insert_code", code), new[] { typeof(object).Assembly.Location, typeof(ExpressionFormula).Assembly.Location, typeof(MathHelper).Assembly.Location });

				var formula = result.Assembly == null
					? new ErrorExpressionFormula(result.Errors.Where(e => e.Type == CompilationErrorTypes.Error).Select(e => e.Message).Join(Environment.NewLine))
					: result.Assembly.GetType("TempExpressionFormula").CreateInstance<ExpressionFormula>(expression, securityIds);

				return formula;
			}
			catch (Exception ex)
			{
				return new ErrorExpressionFormula(ex.ToString());
			}
		}
	}
}