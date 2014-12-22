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

			Console.Write(LocalizedStrings.Str3000);
			var login = Console.ReadLine();

			Console.Write(LocalizedStrings.Str3001);
			var password = Console.ReadLine();

			try
			{
				var terminal = QuikTerminal.Get(path);

				if (!terminal.IsLaunched)
				{
					Console.WriteLine(LocalizedStrings.Str3002);

					terminal.Launch();
					Console.WriteLine(LocalizedStrings.Str3003);
				}
				else
					Console.WriteLine(LocalizedStrings.Str3004);

				if (!terminal.IsConnected)
				{
					terminal.Login(login, password);
					Console.WriteLine(LocalizedStrings.Str3005);
				}

				Console.WriteLine(LocalizedStrings.Str3006);
				Console.ReadLine();

				terminal.Logout();
				Console.WriteLine(LocalizedStrings.Str3007);

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