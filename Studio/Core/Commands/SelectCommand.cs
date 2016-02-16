#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: SelectCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

		public override bool CanRouteToGlobalScope => true;

		public Type InstanceType { get; private set; }
		public object Instance { get; private set; }
		public bool CanEdit => _canEditFunc != null ? _canEditFunc() : _canEdit;
	}

	public class SelectCommand<T> : SelectCommand
	{
		public new T Instance => (T) base.Instance;

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