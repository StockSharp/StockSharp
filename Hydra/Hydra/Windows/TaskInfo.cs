#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Windows.HydraPublic
File: TaskInfo.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Windows
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Hydra.Core;

	class TaskInfo : NotifiableObject
	{
		public TaskInfo(Type task)
		{
			if (task == null)
				throw new ArgumentNullException(nameof(task));

			Task = task;
			Name = task.GetDisplayName();
			Description = task.GetDescription();
			Icon = task.GetIcon();
		}

		public string Name { get; private set; }
		public string Description { get; private set; }
		public Uri Icon { get; private set; }
		public Type Task { get; private set; }

		public event Action Selected;

		private bool _isSelected;

		public bool IsSelected
		{
			get { return _isSelected; }
			set
			{
				_isSelected = value;
				NotifyChanged(nameof(IsSelected));

				Selected.SafeInvoke();
			}
		}

		private bool _isVisible;

		public bool IsVisible
		{
			get { return _isVisible; }
			set
			{
				_isVisible = value;
				NotifyChanged(nameof(IsVisible));
			}
		}
	}
}