namespace StockSharp.Designer.Commands
{
	using System;

	using StockSharp.Studio.Core.Commands;

	class SetDefaultEmulationSettingsCommand : BaseStudioCommand
	{
		public EmulationSettings Settings { get; }

		public SetDefaultEmulationSettingsCommand(EmulationSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			Settings = settings;
		}
	}
}
