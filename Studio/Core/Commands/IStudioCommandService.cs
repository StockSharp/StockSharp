#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: IStudioCommandService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

	using Ecng.Configuration;

	public interface IStudioCommandService
	{
		void Process(object sender, IStudioCommand command, bool isSyncProcess = true);
		bool CanProcess(object sender, IStudioCommand command);

		void Register<TCommand>(object listener, bool guiAsync, Action<TCommand> handler)
			where TCommand : IStudioCommand;

		void Register<TCommand>(object listener, bool guiAsync, Action<TCommand> handler, Func<TCommand, bool> canExecute)
			where TCommand : IStudioCommand;

		void UnRegister<TCommand>(object listener)
			where TCommand : IStudioCommand;

		void Bind(object sender, IStudioCommandScope scope);
		void UnBind(object sender);
	}

	public static class StudioCommandHelper
	{
		public static void Process(this IStudioCommand command, object sender, bool isSyncProcess = false)
		{
			ConfigManager.GetService<IStudioCommandService>().Process(sender, command, isSyncProcess);
		}

		public static void SyncProcess(this IStudioCommand command, object sender)
		{
			ConfigManager.GetService<IStudioCommandService>().Process(sender, command);
		}

		public static bool CanProcess(this IStudioCommand command, object sender)
		{
			return ConfigManager.GetService<IStudioCommandService>().CanProcess(sender, command);
		}
	}
}