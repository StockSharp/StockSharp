namespace StockSharp.Studio.Core.Commands
{
    using StockSharp.Logging;

    public class AddLogListenerCommand : BaseStudioCommand
    {
        public ILogListener Listener { get; private set; }

        public AddLogListenerCommand(ILogListener info)
        {
            Listener = info;
        }
    }

    public class RemoveLogListenerCommand : BaseStudioCommand
    {
        public ILogListener Listener { get; private set; }

        public RemoveLogListenerCommand(ILogListener info)
        {
            Listener = info;
        }
    }
}