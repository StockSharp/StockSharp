namespace StockSharp.Studio.Core.Commands
{
	public interface IStudioCommand
	{
		bool CanRouteToGlobalScope { get; }
	}

	public abstract class BaseStudioCommand : IStudioCommand
	{
		public virtual bool CanRouteToGlobalScope { get { return false; } }
	}
}