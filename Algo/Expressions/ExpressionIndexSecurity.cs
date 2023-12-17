namespace StockSharp.Algo.Expressions
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Compilation.Expressions;
	using Ecng.Compilation;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The index, built of combination of several instruments through mathematical formula <see cref="Expression"/>.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IndexKey,
		Description = LocalizedStrings.IndexSecurityKey)]
	[BasketCode("EI")]
	public class ExpressionIndexSecurity : IndexSecurity
	{
		private readonly AssemblyLoadContextTracker _context = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="ExpressionIndexSecurity"/>.
		/// </summary>
		public ExpressionIndexSecurity()
		{
		}

		/// <summary>
		/// Compiled mathematical formula.
		/// </summary>
		public ExpressionFormula<decimal> Formula { get; private set; } = ExpressionFormula<decimal>.CreateError(LocalizedStrings.ExpressionNotSet);

		/// <summary>
		/// The mathematical formula of index.
		/// </summary>
		[Browsable(false)]
		public string Expression
		{
			get => Formula.Expression;
			set
			{
				if (value.IsEmpty())
				{
					Formula = ExpressionFormula<decimal>.CreateError(LocalizedStrings.ExpressionNotSet);
					return;
					//throw new ArgumentNullException(nameof(value));
				}

				if (ServicesRegistry.TryCompiler is not null)
				{
					Formula = value.Compile(_context);

					_innerSecurityIds.Clear();

					if (Formula.Error.IsEmpty())
					{
						foreach (var v in Formula.Variables)
						{
							_innerSecurityIds.Add(v.ToSecurityId());
						}
					}
					else
						new InvalidOperationException(Formula.Error).LogError();
				}
				else
					new InvalidOperationException(LocalizedStrings.ServiceNotRegistered.Put(nameof(ICompiler))).LogError();
			}
		}

		private readonly CachedSynchronizedList<SecurityId> _innerSecurityIds = new();

		/// <inheritdoc />
		public override IEnumerable<SecurityId> InnerSecurityIds => _innerSecurityIds.Cache;

		/// <inheritdoc />
		public override Security Clone()
		{
			var clone = new ExpressionIndexSecurity { Expression = Expression };
			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString() => Expression;

		/// <inheritdoc />
		protected override string ToSerializedString()
		{
			return Expression;
		}

		/// <inheritdoc />
		protected override void FromSerializedString(string text)
		{
			Expression = text;
		}
	}
}