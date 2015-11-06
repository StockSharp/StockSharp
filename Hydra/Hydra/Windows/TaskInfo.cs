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
				NotifyChanged("IsSelected");
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
				NotifyChanged("IsVisible");
			}
		}
	}
}