namespace StockSharp.Studio.Core.Commands
{
	using System;

	public abstract class SelectCommand : BaseStudioCommand
	{
		private readonly bool _canEdit;
		private readonly Func<bool> _canEditFunc;

		protected SelectCommand(Type instanceType, object instance, bool canEdit)
		{
			_canEdit = canEdit;

			InstanceType = instanceType;
			Instance = instance;
		}

		protected SelectCommand(Type instanceType, object instance, Func<bool> canEdit)
		{
			if (canEdit == null)
				throw new ArgumentNullException(nameof(canEdit));

			_canEditFunc = canEdit;

			InstanceType = instanceType;
			Instance = instance;
		}

		public override bool CanRouteToGlobalScope
		{
			get { return true; }
		}

		public Type InstanceType { get; private set; }
		public object Instance { get; private set; }
		public bool CanEdit { get { return _canEditFunc != null ? _canEditFunc() : _canEdit; } }
	}

	public class SelectCommand<T> : SelectCommand
	{
		public SelectCommand(object instance, bool canEdit)
			: base(typeof(T), instance, canEdit)
		{
		}

		public SelectCommand(object instance, Func<bool> canEdit)
			: base(typeof(T), instance, canEdit)
		{
		}
	}
}