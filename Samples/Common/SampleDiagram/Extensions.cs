namespace SampleDiagram
{
	using System;

	using Ecng.Serialization;

	using SampleDiagram.Layout;

	using StockSharp.Xaml.Diagram;

	static class Extensions
	{
		public static string GetFileName(this CompositionDiagramElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			return element.TypeId.ToString().Replace("-", "_") + ".xml";
		}

		public static void DoIfElse<T>(this object value, Action<T> action, Action elseAction)
			where T : class
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			if (elseAction == null)
				throw new ArgumentNullException(nameof(elseAction));

			var typedValue = value as T;

			if (typedValue != null)
			{
				action(typedValue);
			}
			else
				elseAction();
		}

		public static DockingControl LoadDockingControl(this SettingsStorage settings)
		{
			var type = settings.GetValue<Type>("ControlType");
			var control = (DockingControl)Activator.CreateInstance(type);

			control.Load(settings);

			return control;
		}
	}
}