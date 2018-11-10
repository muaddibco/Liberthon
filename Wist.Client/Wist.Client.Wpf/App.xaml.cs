using CommonServiceLocator;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Wist.Client.Common.Communication;
using Wist.Client.Wpf.ViewModels;
using Wist.Client.Wpf.Views;

namespace Wist.Client.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    public partial class App : Application
    {
        public App()
        {
            //
            // configure global unhandled exception for the project
            //
            Current.DispatcherUnhandledException += (sender, e) =>
            {
                MessageBox.Show(e.Exception.Message, string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ClientBootstrapper clientBootstrapper = new ClientBootstrapper(CancellationToken.None);
            clientBootstrapper.Run();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            LoginWindow loginWindow = new LoginWindow();
            MainWindow = loginWindow;
            LoginViewModel loginViewModel = ServiceLocator.Current.GetInstance<LoginViewModel>();
            loginWindow.DataContext = loginViewModel;
            loginWindow.ShowDialog();

            ShutdownMode = ShutdownMode.OnLastWindowClose;

            if (loginViewModel.IsRegisterUserSelected)
            {
                RegistrationViewModel registrationViewModel = ServiceLocator.Current.GetInstance<RegistrationViewModel>();
                MainWindow = new RegistrationWindow { DataContext = registrationViewModel };
                MainWindow.ShowDialog();
            }
            else if(loginViewModel.IsCreatePollSelected)
            {
                PollViewModel pollViewModel = ServiceLocator.Current.GetInstance<PollViewModel>();
                MainWindow = new PollWindow { DataContext = pollViewModel };
                MainWindow.ShowDialog();
            }
            else if(loginViewModel.IsVoteSelected)
            {
                VoteViewModel voteViewModel = ServiceLocator.Current.GetInstance<VoteViewModel>();
                MainWindow = new VoteWindow { DataContext = voteViewModel };
                MainWindow.ShowDialog();
            }
            else
            {
                Shutdown();
            }
        }
    }


    //public partial class App : PrismApplication
    //{
    //    private readonly ClientBootstrapper _explorerBootstrapper;
    //    private readonly CancellationTokenSource _cancellationTokenSource;
    //    private readonly INetworkSynchronizer _networkSynchronizer;


    //    public App()
    //    {
    //        _cancellationTokenSource = new CancellationTokenSource();
    //        _explorerBootstrapper = new ClientBootstrapper(_cancellationTokenSource.Token);
    //    }

    //    protected override IContainerExtension CreateContainerExtension()
    //    {
    //        UnityContainerExtension unityContainerExtension = new UnityContainerExtension(_explorerBootstrapper.CreateContainer());

    //        return unityContainerExtension;
    //    }

    //    protected override void ConfigureServiceLocator()
    //    {
    //        _explorerBootstrapper.ConfigureServiceLocator();
    //    }

    //    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    //    {
    //        _explorerBootstrapper.ConfigureContainer();
    //    }

    //    public override void Initialize()
    //    {
    //        base.Initialize();

    //        _networkSynchronizer.Initialize(_cancellationTokenSource.Token);
    //    }

    //    protected override void InitializeShell(Window shell)
    //    {
    //        base.InitializeShell(shell);
    //    }
       
    //    protected override void OnExit(ExitEventArgs e)
    //    {
    //        _cancellationTokenSource.Cancel();

    //        base.OnExit(e);
    //    }

    //    protected override Window CreateShell()
    //    {
    //        _explorerBootstrapper.Initialize();

    //        _networkSynchronizer.Start();

    //        return ServiceLocator.Current.GetInstance<MainWindow>();
    //    }
    //}
}
