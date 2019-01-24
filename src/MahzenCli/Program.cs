using Mono.Options;
using System;

namespace MahzenCli
{
    class Program
    {
        private const string Localhost = "127.0.0.1:6970";

        static void Main(string[] args)
        {
            var nodeIpAddresses = Localhost;
            var showHelp = false;
            var p = new OptionSet() {
                "Usage: MahzenCli [OPTIONS]+",
                "",
                "Options:",
                { "n|nodes=", "One or more ip:port settings of the nodes in the cluster.",
                  v => nodeIpAddresses = v ?? Localhost },
                { "h|help",  "Show the help",
                  v => showHelp = v != null },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"{e.Message}\r\n Try `MahzenCli --help' for more information.");
                return;
            }

            if (showHelp)
            {
                p.WriteOptionDescriptions(Console.Out);
                return;
            }


            Console.ReadKey();
        }
    }
}
