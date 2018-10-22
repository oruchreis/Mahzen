using System;
using System.Collections.Generic;
using System.IO;
using Mahzen.Configuration;
using Microsoft.Extensions.Configuration;
using Mono.Options;
using Serilog;

namespace Mahzen
{
    class Program
    {
        private const string DefaultConfigPath = "./config.json";
        public static Settings Settings;
        static void Main(string[] args)
        {
            var configPath = DefaultConfigPath;
            var showHelp = false;
            var p = new OptionSet() {
                "Usage: Mahzen [OPTIONS]+",
                "",
                "Options:",
                { "c|config=", "The configuration file.",
                  v => configPath = v ?? DefaultConfigPath },
                { "h|help",  "Show the help",
                  v => showHelp = v != null },
            };

            List<string> arguments;
            try
            {
                arguments = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"{e.Message}\r\n Try `Mahzen --help' for more information.");
                return;
            }

            if (showHelp)
            {
                p.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (!File.Exists(configPath))
            {
                Console.WriteLine("Config file does not exists: {0}", configPath);
                return;
            }

            try
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile(configPath, optional: false)
                    .AddEnvironmentVariables("Mahzen:")
                    .Build();
                Settings = config.Get<Settings>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            Serilog.Debugging.SelfLog.Enable(Console.Out);

            var loggerConfiguration = new LoggerConfiguration();

            if (Settings.Logging.IsEnabled)
            {
                try
                {
                    if (!Directory.Exists(Settings.Logging.Path))
                    {
                        Directory.CreateDirectory(Settings.Logging.Path);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                loggerConfiguration = loggerConfiguration.WriteTo.Async(a =>
                    a.File(
                        $"{Settings.Logging.Path}/Mahzen.log",
                        Settings.Logging.MinimumLevel,
                        rollingInterval: Settings.Logging.RollingInterval,
                        rollOnFileSizeLimit: Settings.Logging.RollOnFileSize,
                        fileSizeLimitBytes: Settings.Logging.RollingFileSizeBytes,
                        retainedFileCountLimit: Settings.Logging.RetainedFileCount));
            }

            Log.Logger = loggerConfiguration
                .WriteTo.Console(Settings.StdoutLevel)
                .CreateLogger();               

            Log.Information("Mahzen is starting...");


        }
    }
}
