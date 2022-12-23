namespace StockSharp.Algo.Expressions
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

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
	[DisplayNameLoc(LocalizedStrings.IndexKey)]
	[DescriptionLoc(LocalizedStrings.Str728Key)]
	[BasketCode("EI")]
	public class ExpressionIndexSecurity : IndexSecurity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExpressionIndexSecurity"/>.
		/// </summary>
		public ExpressionIndexSecurity()
		{
		}

		/// <summary>
		/// Compiled mathematical formula.
		/// </summary>
		public ExpressionFormula Formula { get; private set; } = ExpressionHelper.CreateError(LocalizedStrings.ExpressionNotSet);

		/// <summary>
		/// The mathematical formula of index.
		/// </summary>
		[Browsable(false)]
		public string Expression
		{
			get => Formula.Expression;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (ServicesRegistry.TryCompiler is not null
#pragma warning disable CS0612 // Type or member is obsolete
					|| ServicesRegistry.TryCompilerService is not null
#pragma warning restore CS0612 // Type or member is obsolete
				)
				{
					Formula = value.Compile();

					_innerSecurityIds.Clear();

					if (Formula.Error.IsEmpty())
					{
						foreach (var id in Formula.Identifiers)
						{
							_innerSecurityIds.Add(id.ToSecurityId());
						}
					}
					else
						new InvalidOperationException(Formula.Error).LogError();
				}
				else
					new InvalidOperationException($"Service {nameof(ICompiler)} is not initialized.").LogError();
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