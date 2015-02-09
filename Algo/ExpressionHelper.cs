namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;

	using Ecng.Common;

	using NCalc;
	using NCalc.Domain;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Вспомогательный класс для работы с <see cref="ExpressionIndexSecurity"/>.
	/// </summary>
	[CLSCompliant(false)]
	public static class ExpressionHelper
	{
		private static readonly Regex _regex = new Regex(@"(?<secId>\w+@\w+)");
		
		private static IEnumerable<string> GetSecurityIds(string expression)
		{
			return
				from Match match in _regex.Matches(expression)
					where match.Success
				select match.Groups["secId"].Value;
		}

		/// <summary>
		/// Экранировать математическую формулу от идентификаторов инструментов <see cref="Security.Id"/>.
		/// </summary>
		/// <param name="expression">Исходные текст.</param>
		/// <returns>Экранированный текст.</returns>
		public static string Encode(string expression)
		{
			foreach (var secId in ExpressionHelper.GetSecurityIds(expression).Distinct(StringComparer.CurrentCultureIgnoreCase))
			{
				expression = expression.Replace(secId, "[{0}]".Put(secId));
			}

			return expression;
		}

		/// <summary>
		/// Получить все <see cref="Security.Id"/> из математической формулы.
		/// </summary>
		/// <param name="expression">Математическая формула.</param>
		/// <returns>Идентификаторы инструментов.</returns>
		public static IEnumerable<string> GetSecurityIds(this Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");

			return expression.ParsedExpression.GetSecurityIds().Distinct(StringComparer.CurrentCultureIgnoreCase);
		}

		private static IEnumerable<string> GetSecurityIds(this LogicalExpression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");

			if (expression is BinaryExpression)
			{
				var binary = (BinaryExpression)expression;
				return binary.LeftExpression.GetSecurityIds().Concat(binary.RightExpression.GetSecurityIds());
			}
			else if (expression is UnaryExpression)
			{
				var unary = (UnaryExpression)expression;
				return unary.Expression.GetSecurityIds();
			}
			else if (expression is TernaryExpression)
			{
				var ternary = (TernaryExpression)expression;
				return ternary.LeftExpression.GetSecurityIds()
					.Concat(ternary.RightExpression.GetSecurityIds())
					.Concat(ternary.MiddleExpression.GetSecurityIds());
			}
			else if (expression is Function)
			{
				var func = (Function)expression;
				return func.Expressions.SelectMany(e => e.GetSecurityIds()).Concat(func.Identifier.GetSecurityIds());
			}
			else if (expression is Identifier)
			{
				var id = (Identifier)expression;
				return new[] { id.Name };
			}
			else
				return Enumerable.Empty<string>();
		}
	}
}