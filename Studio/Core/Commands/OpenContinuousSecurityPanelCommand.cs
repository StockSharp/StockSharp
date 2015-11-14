namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.Algo;

	public class OpenContinuousSecurityPanelCommand : BaseStudioCommand
	{
		public ContinuousSecurity Security { get; private set; }

		public OpenContinuousSecurityPanelCommand(ContinuousSecurity security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			Security = security;
		}
	}
}