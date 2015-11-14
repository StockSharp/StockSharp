namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class BindCommand<T> : BaseStudioCommand
		where T : class
	{
		public T Source { get; private set; }

		public IStudioControl Control { get; private set; }

		public BindCommand(T source, IStudioControl control = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			Source = source;
			Control = control;
		}

		public bool CheckControl(IStudioControl control)
		{
			//если не задан контрол, то считаем, что привязать страегию надо для всех контролов
			return Control == control;
		}
	}

	public class BindStrategyCommand : BindCommand<StrategyContainer>
	{
		public BindStrategyCommand(StrategyContainer source, IStudioControl control = null)
			: base(source, control)
		{
		}
	}

	public class BindConnectorCommand : BindCommand<IConnector>
	{
		public BindConnectorCommand(IConnector source, IStudioControl control = null)
			: base(source, control)
		{
		}
	}
}