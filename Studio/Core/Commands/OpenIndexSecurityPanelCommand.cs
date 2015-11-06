namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.Algo;

	public class OpenIndexSecurityPanelCommand : BaseStudioCommand
	{
		public ExpressionIndexSecurity Security { get; private set; }

		public OpenIndexSecurityPanelCommand(ExpressionIndexSecurity security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			Security = security;
		}
	}
}