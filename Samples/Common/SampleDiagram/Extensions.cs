#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleDiagram
{
	using System;

	using StockSharp.Xaml.Diagram;

	static class Extensions
	{
		public static string GetFileName(this CompositionDiagramElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			return element.TypeId.ToString().Replace("-", "_") + ".xml";
		}

		public static void DoIfElse<T>(this object value, Action<T> action, Action elseAction)
			where T : class
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			if (elseAction == null)
				throw new ArgumentNullException(nameof(elseAction));

			var typedValue = value as T;

			if (typedValue != null)
			{
				action(typedValue);
			}
			else
				elseAction();
		}
	}
}