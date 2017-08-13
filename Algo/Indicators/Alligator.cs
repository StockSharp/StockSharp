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

	using StockSharp.Localization;

	/// <summary>
	/// Alligator.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/alligator.
	/// </remarks>
	[DisplayNameLoc(LocalizedStrings.Str837Key)]
	[DescriptionLoc(LocalizedStrings.Str837Key, true)]
	public class Alligator : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Alligator"/>.
		/// </summary>
		public Alligator()
			: this(new AlligatorLine { Length = 13, Shift = 8 }, new AlligatorLine { Length = 8, Shift = 5 }, new AlligatorLine { Length = 5, Shift = 3 })
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
		[DisplayNameLoc(LocalizedStrings.Str838Key)]
		[DescriptionLoc(LocalizedStrings.Str838Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public AlligatorLine Jaw { get; }

		/// <summary>
		/// Teeth.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.Str839Key)]
		[DescriptionLoc(LocalizedStrings.Str839Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public AlligatorLine Teeth { get; }

		/// <summary>
		/// Lips.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.Str840Key)]
		[DescriptionLoc(LocalizedStrings.Str840Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public AlligatorLine Lips { get; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => Jaw.IsFormed;
	}
}
