using StockSharp.Studio.Core.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSharp.Terminal.Fakes
{
	public class FakeStudioCommandService : IStudioCommandService
	{
		public void Bind(object sender, IStudioCommandScope scope)
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

		public void UnBind(object sender)
		{

		}

		public void UnRegister<TCommand>(object listener) where TCommand : IStudioCommand
		{

		}
	}
}
