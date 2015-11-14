namespace StockSharp.Studio.Controls.Commands
{
	using System;

	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml.Diagram;

	public class OpenCompositionCommand : BaseStudioCommand
	{
		public CompositionDiagramElement Composition { get; private set; }

		public OpenCompositionCommand(CompositionDiagramElement composition)
		{
			if (composition == null) 
				throw new ArgumentNullException(nameof(composition));

			Composition = composition;
		}
	}
}