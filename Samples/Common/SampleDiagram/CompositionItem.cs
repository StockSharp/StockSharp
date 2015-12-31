#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: CompositionItem.cs
Created: 2015, 12, 9, 6:53 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleDiagram
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Xaml.Diagram;

	public enum CompositionType
	{
		[EnumDisplayNameLoc(LocalizedStrings.Str3050Key)]
		Composition,

		[EnumDisplayNameLoc(LocalizedStrings.Str1355Key)]
		Strategy
	}

	public class CompositionItem : Equatable<CompositionItem>
	{
		public CompositionType Type { get; }

		public CompositionDiagramElement Element { get; }

		public string Key => Element.TypeId.ToString();

		public CompositionItem(CompositionType type, CompositionDiagramElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			Type = type;
			Element = element;
		}

		public override CompositionItem Clone()
		{
			return new CompositionItem(Type, Element);
		}

		protected override bool OnEquals(CompositionItem other)
		{
			return Type == other.Type && Element == other.Element;
		}
	}
}