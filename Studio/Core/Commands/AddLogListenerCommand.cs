#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: AddLogListenerCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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