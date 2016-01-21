using StockSharp.Studio.Core.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StockSharp.Terminal.Interfaces;

namespace StockSharp.Terminal.Services
{
	public class TerminalCommandService : ITerminalCommandService
	{
		public void Bind(object sender, IStudioCommandScope scope)
		{
			
		}

		public void UnBind(object sender)
		{

		}

		public bool CanProcess(object sender, IStudioCommand command)
		{
			return true;
		}

		public void Process(object sender, IStudioCommand command, bool isSyncProcess = true)
		{

		}

		public void Register<TCommand>(object listener, bool guiAsync, Action<TCommand> handler) where TCommand : IStudioCommand
		{

		}

		public void Register<TCommand>(object listener, bool guiAsync, Action<TCommand> handler, Func<TCommand, bool> canExecute) where TCommand : IStudioCommand
		{

		}

		public void UnRegister<TCommand>(object listener) where TCommand : IStudioCommand
		{

		}
	}
}
