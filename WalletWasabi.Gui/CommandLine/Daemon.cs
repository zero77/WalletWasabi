﻿using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using WalletWasabi.Helpers;
using WalletWasabi.KeyManagement;
using WalletWasabi.Logging;

namespace WalletWasabi.Gui.CommandLine
{
	public static class Daemon
	{
		public static void Run(string[] args, out bool continueWithGui)
		{
			continueWithGui = true;
			var silent = false;

			var showHelp = false;
			var showVersion = false;
			LogLevel? logLevel = null;
			string walletName = null;
			var doMix = false;

			try
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					Native.AttachParentConsole();
					Console.WriteLine();
				}

				var options = new OptionSet() {
					{ "v|version", "Displays Wasabi version and exit.", x => showVersion = x != null},
					{ "h|help", "Displays help page and exit.", x => showHelp = x != null},
					{ "s|silent", "Do not log to the standard outputs.", x => silent = x != null},
					{ "l|loglevel=", "Sets the level of verbosity for the log TRACE|INFO|WARNING|DEBUG|ERROR.", x => {
						var normalized = x?.ToLower()?.Trim();
						if(normalized == "info") logLevel = LogLevel.Info;
						else if(normalized == "warning")  logLevel = LogLevel.Warning;
						else if(normalized == "error") logLevel = LogLevel.Error;
						else if(normalized == "trace") logLevel = LogLevel.Trace;
						else if(normalized == "debug") logLevel = LogLevel.Debug;
						else {
							Console.WriteLine("ERROR: Log level not recognized.");
							showHelp = true;
						}
					}},
					{ "m|mix", "Start mixing without the GUI with the specified wallet.", x => doMix = x != null},
					{ "w|wallet=", "The specified wallet file.", x => {
						walletName = x?.ToLower()?.Trim();
					}}
				};
				try
				{
					var extras = options.Parse(args);
					if (extras.Count > 0)
					{
						showHelp = true;
					}
				}
				catch (OptionException)
				{
					continueWithGui = false;
					Console.WriteLine("Option not recognized.");
					Console.WriteLine();
					ShowHelp(options);
					return;
				}
				if (showHelp)
				{
					continueWithGui = false;
					ShowHelp(options);
					return;
				}
				else if (showVersion)
				{
					continueWithGui = false;
					ShowVersion();
					return;
				}
			}
			finally
			{
				if (silent)
				{
					Native.DettachParentConsole();
				}
			}

			Logger.InitializeDefaults(Path.Combine(Global.DataDir, "Logs.txt"));

			if (logLevel.HasValue)
			{
				Logger.SetMinimumLevel(logLevel.Value);
			}
			if (silent)
			{
				Logger.Modes.Remove(LogMode.Console);
				Logger.Modes.Remove(LogMode.Debug);
			}
			else
			{
				Logger.Modes.Add(LogMode.Console);
				Logger.Modes.Add(LogMode.Debug);
			}

			KeyManager keyManager = null;
			if (walletName != null)
			{
				continueWithGui = false;

				var walletFullPath = Global.GetWalletFullPath(walletName);
				var walletBackupFullPath = Global.GetWalletBackupFullPath(walletName);
				if (!File.Exists(walletFullPath) && !File.Exists(walletBackupFullPath))
				{
					// The selected wallet is not available any more (someone deleted it?).
					Logger.LogCritical("The selected wallet doesn't exsist, did you delete it?", nameof(Daemon));
					return;
				}

				try
				{
					keyManager = Global.LoadKeyManager(walletFullPath, walletBackupFullPath);
				}
				catch (Exception ex)
				{
					Logger.LogCritical(ex, nameof(Daemon));
					return;
				}
			}

			if (doMix)
			{
				continueWithGui = false;

				if (keyManager == null)
				{
					Logger.LogCritical("Wallet was not supplied. Add --wallet {WalletName}", nameof(Daemon));
					return;
				}
			}
		}

		private static void ShowVersion()
		{
			Console.WriteLine($"Wasabi Client Version: {Constants.ClientVersion}");
			Console.WriteLine($"Compatible Coordinator Version: {Constants.BackendMajorVersion}");
		}

		private static void ShowHelp(OptionSet p)
		{
			ShowVersion();
			Console.WriteLine();
			Console.WriteLine("Usage: wassabee [OPTIONS]+");
			Console.WriteLine("Launches Wasabi Wallet.");
			Console.WriteLine();
			Console.WriteLine("Options:");
			p.WriteOptionDescriptions(Console.Out);
		}
	}
}
