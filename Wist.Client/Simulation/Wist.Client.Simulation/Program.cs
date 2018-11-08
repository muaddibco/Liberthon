using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wist.Core.ExtensionMethods;
using System.Threading;
using System.Threading.Tasks;
using Wist.Client.Common;
using Wist.Core.Cryptography;

namespace Wist.Client.Simulation
{
    class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            XmlConfigurator.Configure();

            _log.Info("===== NODE STARTED =====");

            ConfigureUnhandledExceptions();

            ClientBootstrapper nodeBootstrapper = new ClientBootstrapper(cancellationTokenSource.Token);
            nodeBootstrapper.Run(new Dictionary<string, string> { { "secretKey", "1F0B7DBB567EFC99060EC69DD60130B4364E36B7A88248DD234285B3860F63C3" } });
            //nodeBootstrapper.Run(new Dictionary<string, string> { { "secretKey", CryptoHelper.GetRandomSeed().ToHexString() } });

            string command = null;
            do
            {
                command = System.Console.ReadLine();
            } while (command?.ToLower() != "exit");

            cancellationTokenSource.Cancel();
        }

        private static void ConfigureUnhandledExceptions()
        {
            if (Process.GetCurrentProcess().ProcessName.EndsWith(".vshost")) return;

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                _log.Error("Unhandled exception caught", args.Exception);
                args.SetObserved();
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    _log.Error("Unhandled exception caught", ex);
                }
                else
                {
                    _log.Error(args.ExceptionObject?.ToString());
                }
            };
        }
    }
}
