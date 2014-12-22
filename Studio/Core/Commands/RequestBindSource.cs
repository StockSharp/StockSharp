namespace StockSharp.Studio.Core.Commands
{
	public class RequestBindSource : BaseStudioCommand
	{
		public IStudioControl Control { get; private set; }

		public RequestBindSource(IStudioControl control)
		{
			Control = control;
		}
	}
}