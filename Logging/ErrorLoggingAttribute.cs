namespace StockSharp.Logging
{
	using System;
	using System.Collections.ObjectModel;
	using System.ServiceModel;
	using System.ServiceModel.Channels;
	using System.ServiceModel.Description;
	using System.ServiceModel.Dispatcher;

	/// <summary>
	/// Атрибут для WCF сервер, который автоматически записывает все ошибки в <see cref="LoggingHelper.LogError"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class ErrorLoggingAttribute : Attribute, IServiceBehavior
	{
		private sealed class ErrorHandler : IErrorHandler
		{
			private ErrorHandler()
			{
			}

			private static readonly Lazy<ErrorHandler> _instance = new Lazy<ErrorHandler>(() => new ErrorHandler());

			public static ErrorHandler Instance
			{
				get { return _instance.Value; }
			}

			public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
			{
			}

			public bool HandleError(Exception error)
			{
				error.LogError();
				return true;
			}
		}

		void IServiceBehavior.Validate(ServiceDescription description, ServiceHostBase serviceHostBase)
		{
		}

		void IServiceBehavior.AddBindingParameters(ServiceDescription description, ServiceHostBase serviceHostBase,
												   Collection<ServiceEndpoint> endpoints,
												   BindingParameterCollection parameters)
		{
		}

		void IServiceBehavior.ApplyDispatchBehavior(ServiceDescription description, ServiceHostBase serviceHostBase)
		{
			foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
				channelDispatcher.ErrorHandlers.Add(ErrorHandler.Instance);
		}
	}
}