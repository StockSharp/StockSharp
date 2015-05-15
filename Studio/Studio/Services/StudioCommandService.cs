namespace StockSharp.Studio.Services
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Logging;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	using CommandTuple = System.Tuple<System.Action<Core.Commands.IStudioCommand>, System.Func<Core.Commands.IStudioCommand, bool>, bool>;

	class StudioCommandService : IStudioCommandService
	{
		private sealed class Scope : IStudioCommandScope
		{
			private readonly string _name;

			public Scope(string name)
			{
				_name = name;
			}

			public override string ToString()
			{
				return _name;
			}
		}

		private readonly IStudioCommandScope _globalScope = new Scope("global");
		private readonly IStudioCommandScope _undefinedScope = new Scope("undefined");
		private readonly SynchronizedDictionary<Type, Dictionary<IStudioCommandScope, CachedSynchronizedDictionary<object, CommandTuple>>> _handlers = new SynchronizedDictionary<Type, Dictionary<IStudioCommandScope, CachedSynchronizedDictionary<object, CommandTuple>>>();
		private readonly SynchronizedPairSet<object, IStudioCommandScope> _binds = new SynchronizedPairSet<object, IStudioCommandScope>();
		private readonly SynchronizedDictionary<object, IStudioCommandScope> _scopes = new SynchronizedDictionary<object, IStudioCommandScope>();
		private readonly BlockingQueue<Tuple<IStudioCommand, IStudioCommandScope>> _queue = new BlockingQueue<Tuple<IStudioCommand, IStudioCommandScope>>();

		public StudioCommandService()
		{
			ThreadingHelper
				.Thread(Process)
				.Background(true)
				.Culture(CultureInfo.InvariantCulture)
				.Name("Studio command service thread")
				.Launch();
		}

		bool IStudioCommandService.CanProcess(object sender, IStudioCommand command)
		{
			var handlers = TryGetHandlers(command);

			if (handlers == null)
				return false;

			var scope = sender is IStudioCommandScope ? _globalScope : GetScope(sender);

			if (scope == _undefinedScope)
				return false;

			var scopeHandlers = handlers.TryGetValue(scope) ?? (command.CanRouteToGlobalScope ? handlers.TryGetValue(_globalScope) : null);

			return scopeHandlers != null && scopeHandlers.CachedValues.All(tuple => tuple.Item2 == null || tuple.Item2(command));
		}

		void IStudioCommandService.Process(object sender, IStudioCommand command, bool isSyncProcess)
		{
			if (TryGetHandlers(command) == null)
				return;

			var scope = sender is IStudioCommandScope ? _globalScope : GetScope(sender);

			if (scope == _undefinedScope)
				return;

			if (isSyncProcess)
			{
				if (Thread.CurrentThread != GuiDispatcher.GlobalDispatcher.Dispatcher.Thread)
					throw new ArgumentException(LocalizedStrings.Str3596);

				ProcessCommand(command, scope);
				return;
			}

			_queue.Enqueue(Tuple.Create(command, scope));
		}

		private void Process()
		{
			while (true)
			{
				try
				{
					Tuple<IStudioCommand, IStudioCommandScope> item;

					if (!_queue.TryDequeue(out item))
						break;

					ProcessCommand(item.Item1, item.Item2);
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}
		}

		private void ProcessCommand(IStudioCommand command, IStudioCommandScope scope)
		{
			var handlers = TryGetHandlers(command);

			if (handlers == null)
				return;

			var scopeHandlers = handlers.TryGetValue(scope) ?? (command.CanRouteToGlobalScope ? handlers.TryGetValue(_globalScope) : null);

			if (scopeHandlers == null)
				return;

			var guiAsyncActions = new List<Tuple<CommandTuple, IStudioCommand>>();

			foreach (var tuple in scopeHandlers.CachedValues)
			{
				if (!tuple.Item3)
				{
					ProcessCommand(tuple, command);
				}
				else
					guiAsyncActions.Add(Tuple.Create(tuple, command));
			}

			if (!guiAsyncActions.IsEmpty())
				GuiDispatcher.GlobalDispatcher.AddAction(() => guiAsyncActions.ForEach(t => ProcessCommand(t.Item1, t.Item2)));
		}

		private static void ProcessCommand(CommandTuple tuple, IStudioCommand command)
		{
			try
			{
				tuple.Item1(command);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		void IStudioCommandService.Register<TCommand>(object listener, bool guiAsync, Action<TCommand> handler)
		{
			_handlers.SafeAdd(typeof(TCommand)).SafeAdd(GetScope(listener))[listener] = new CommandTuple(cmd => handler((TCommand)cmd), null, guiAsync);
		}

		void IStudioCommandService.Register<TCommand>(object listener, bool guiAsync, Action<TCommand> handler, Func<TCommand, bool> canExecute)
		{
			_handlers.SafeAdd(typeof(TCommand)).SafeAdd(GetScope(listener))[listener] = new CommandTuple(cmd => handler((TCommand)cmd), cmd => canExecute((TCommand)cmd), guiAsync);
		}

		void IStudioCommandService.UnRegister<TCommand>(object listener)
		{
			var handlers = _handlers.TryGetValue(typeof(TCommand));

			if (handlers == null)
				return;

			var scopeHandler = handlers.TryGetValue(GetScope(listener));

			if (scopeHandler == null)
				return;

			scopeHandler.Remove(listener);
		}

		void IStudioCommandService.Bind(object sender, IStudioCommandScope scope)
		{
			if (sender == null)
				throw new ArgumentNullException("sender");

			if (scope == null)
				throw new ArgumentNullException("scope");

			_binds.Add(sender, scope);

			var s = _scopes.TryGetValue(sender);
			if (s != null)
			{
				ReplaceHandlersScope(sender, s, scope);
			}

			_scopes[sender] = scope;
		}

		void IStudioCommandService.UnBind(object sender)
		{
			if (sender == null)
				throw new ArgumentNullException("sender");

			_binds.Remove(sender);
			_scopes.Remove(sender);
		}

		private IStudioCommandScope GetScope(object listener)
		{
			if (listener == null)
				throw new ArgumentNullException("listener");

			var sc = listener as StrategyContainer;

			if (sc != null)
				listener = sc.Strategy;

			return _scopes.SafeAdd(listener, InternalGetScope);
		}

		private IStudioCommandScope InternalGetScope(object listener)
		{
			if (listener == null)
				throw new ArgumentNullException("listener");

			var scope = listener as IStudioCommandScope;

			if (scope != null)
				return scope;

			scope = _binds.TryGetValue(listener);

			if (scope != null)
				return scope;

			var ctrl = listener as DependencyObject;

			if (ctrl == null || ctrl is Window)
				return _globalScope;

			var parent = LogicalTreeHelper.GetParent(ctrl);

			if (parent == null)
			{
				((FrameworkElement)ctrl).Loaded += OnUserControlLoaded;
				return _undefinedScope;
			}

			return InternalGetScope(parent);
		}

		private void OnUserControlLoaded(object sender, RoutedEventArgs e)
		{
			var studioControl = sender as IStudioControl;
			var contentControl = sender as ContentControl;

			while (contentControl != null && studioControl == null)
			{
				studioControl = contentControl.Content as IStudioControl;
				contentControl = contentControl.Content as ContentControl;
			}

			if (studioControl == null)
				throw new InvalidOperationException(LocalizedStrings.Str3597Params.Put(sender));

			var scope = _scopes[studioControl];

			if (scope != _undefinedScope)
				return;

			scope = InternalGetScope(LogicalTreeHelper.GetParent((DependencyObject)studioControl));
			_scopes[sender] = scope;

			ReplaceHandlersScope(sender, _undefinedScope, scope);
		}

		private void ReplaceHandlersScope(object sender, IStudioCommandScope oldScope, IStudioCommandScope newScope)
		{
			lock (_handlers.SyncRoot)
			{
				foreach (var handlers in _handlers)
				{
					var scopeHandlers = handlers.Value.TryGetValue(oldScope);

					if (scopeHandlers == null)
						continue;

					var handler = scopeHandlers.TryGetValue(sender);

					if (handler != null)
					{
						scopeHandlers.Remove(sender);
						handlers.Value.SafeAdd(newScope)[sender] = handler;
					}
				}
			}
		}

		private Dictionary<IStudioCommandScope, CachedSynchronizedDictionary<object, CommandTuple>> TryGetHandlers(IStudioCommand command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			var commandType = command.GetType();

			var handlers = _handlers.TryGetValue(commandType);

			if (handlers != null)
				return handlers;

			var baseType = _handlers.Keys.FirstOrDefault(commandType.IsSubclassOf);

			return baseType != null
				? _handlers.TryGetValue(baseType)
				: null;
		}
	}
}