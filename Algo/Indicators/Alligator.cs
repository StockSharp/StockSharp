#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Alligator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Alligator.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/IndicatorAlligator.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.Str837Key,
		Description = LocalizedStrings.Str837Key)]
	[Doc("topics/IndicatorAlligator.html")]
	public class Alligator : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Alligator"/>.
		/// </summary>
		public Alligator()
			: this(new() { Length = 13, Shift = 8 }, new() { Length = 8, Shift = 5 }, new() { Length = 5, Shift = 3 })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Alligator"/>.
		/// </summary>
		/// <param name="jaw">Jaw.</param>
		/// <param name="teeth">Teeth.</param>
		/// <param name="lips">Lips.</param>
		public Alligator(AlligatorLine jaw, AlligatorLine teeth, AlligatorLine lips)
			: base(jaw, teeth, lips)
		{
			Jaw = jaw;
			Teeth = teeth;
			Lips = lips;
		}

		/// <summary>
		/// Jaw.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str838Key,
			Description = LocalizedStrings.Str838Key,
			GroupName = LocalizedStrings.GeneralKey)]
		[Browsable(true)]
		public AlligatorLine Jaw { get; }

		/// <summary>
		/// Teeth.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str839Key,
			Description = LocalizedStrings.Str839Key,
			GroupName = LocalizedStrings.GeneralKey)]
		[Browsable(true)]
		public AlligatorLine Teeth { get; }

		/// <summary>
		/// Lips.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str840Key,
			Description = LocalizedStrings.Str840Key,
			GroupName = LocalizedStrings.GeneralKey)]
		[Browsable(true)]
		public AlligatorLine Lips { get; }

		/// <inheritdoc />
		protected override bool CalcIsFormed() => Jaw.IsFormed;

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $" J={Jaw.Length} T={Teeth.Length} L={Lips.Length}";
	}
}
