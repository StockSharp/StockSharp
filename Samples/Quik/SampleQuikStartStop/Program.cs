namespace SampleQuikStartStop
{
	using System;
	using StockSharp.Quik;
	using StockSharp.Localization;

	class Program
	{
		static void Main()
		{
			Console.Write(LocalizedStrings.Str2999);
			var path = Console.ReadLine();

			Console.Write(LocalizedStrings.EnterLogin + ": ");
			var login = Console.ReadLine();

			Console.Write(LocalizedStrings.EnterPassword + ": ");
			var password = Console.ReadLine();

			try
			{
				var terminal = QuikTerminal.Get(path);

				if (!terminal.IsLaunched)
				{
					Console.WriteLine(LocalizedStrings.QuikStarting);

					terminal.Launch();
					Console.WriteLine(LocalizedStrings.QuikLaunched);
				}
				else
					Console.WriteLine(LocalizedStrings.QuikFound);

				if (!terminal.IsConnected)
				{
					terminal.Login(login, password);
					Console.WriteLine(LocalizedStrings.AuthorizationSuccessful);
				}

				Console.WriteLine(LocalizedStrings.PressEnter);
				Console.ReadLine();

				terminal.Logout();
				Console.WriteLine(LocalizedStrings.QuikDisconnected);

				terminal.Exit();
				Console.WriteLine(LocalizedStrings.Str3008);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}