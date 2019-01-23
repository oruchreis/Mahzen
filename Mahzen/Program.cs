using System;
using System.IO;
using System.Threading;
using Mahzen.Configuration;
using Mahzen.Core;
using Mahzen.Listener;
using Microsoft.Extensions.Configuration;
using Mono.Options;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Mahzen
{
    class Program
    {
        private const string DefaultConfigPath = "./config.json";
        static void Main(string[] args)
        {            
            Thread.CurrentThread.Name = "MainThread";
            
            //getting commandline arguments
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

            try
            {
                p.Parse(args);
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
            
            
            //getting config file from configPath
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
                Settings.Get = config.Get<Settings>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            
            //configuring logging
            Serilog.Debugging.SelfLog.Enable(Console.Out);

            var loggerConfiguration = new LoggerConfiguration();

            if (Settings.Get.Logging.IsEnabled)
            {
                try
                {
                    if (!Directory.Exists(Settings.Get.Logging.Path))
                    {
                        Directory.CreateDirectory(Settings.Get.Logging.Path);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                loggerConfiguration = loggerConfiguration.WriteTo.Async(a =>
                    a.File(
                        $"{Settings.Get.Logging.Path}/Mahzen.log",
                        Settings.Get.Logging.MinimumLevel,
                        rollingInterval: Settings.Get.Logging.RollingInterval,
                        rollOnFileSizeLimit: Settings.Get.Logging.RollOnFileSize,
                        fileSizeLimitBytes: Settings.Get.Logging.RollingFileSizeBytes,
                        retainedFileCountLimit: Settings.Get.Logging.RetainedFileCount));
            }

            Log.Logger = loggerConfiguration
                .WriteTo.Console(Settings.Get.StdoutLevel, theme: AnsiConsoleTheme.Code)
                .CreateLogger();               

            //starting...
            Log.Information("Mahzen is starting...");
            
            //Default command invokers
            CommandDispatcher.RegisterInvoker(
                //order is important
                new NodeCommandInvoker(),
                new ClusterCommandInvoker(),
                new StorageCommandInvoker());

            //main cancel token for the app
            var applicationCancelToken = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                Log.Information("Cancellation is requested");
                applicationCancelToken.Cancel();
            };

#pragma warning disable 4014
            //we are not awaiting these methods, it must start on the thread pool instead of this main thread.
            new NodeTalkListener(applicationCancelToken.Token).StartAsync();
            new NodeListener(applicationCancelToken.Token).StartAsync();
#pragma warning restore 4014
            
            Log.Information("Mahzen is started.");
            
            //we are blocking main app thread until the app cancel token is triggered.
            WaitHandle.WaitAny(new[] {applicationCancelToken.Token.WaitHandle});
            
            //cancel token is requested, stopping.
            Log.Information("Mahzen is stopped.");
        }
    }
}
