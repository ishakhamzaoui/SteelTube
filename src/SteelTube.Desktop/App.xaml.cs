using System;
using System.Windows;
using System.Windows.Threading;
using SteelTube.Desktop.ViewModels;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop
{
    /// <summary>
    /// Startup sequence: open/create the SQLite database (SAD 23, SAD 55
    /// "Initialize Database"), then show the shell. There is no
    /// StartupUri in App.xaml on purpose -- MainWindow needs a
    /// <see cref="CompositionRoot"/> to exist first, and building one
    /// requires awaiting the database.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private CompositionRoot _root;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // SAD 52: exceptions the UI doesn't explicitly handle must
            // still never show a raw stack trace to a normal user.
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            try
            {
                _root = await CompositionRoot.CreateAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "SteelTube could not open its database and cannot continue." + Environment.NewLine + Environment.NewLine + ex.Message,
                    "SteelTube - Startup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(_root)
            };
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                "Something went wrong and the last action could not be completed." + Environment.NewLine + Environment.NewLine + e.Exception.Message,
                "SteelTube - Unexpected error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            e.Handled = true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _root?.Dispose();
            base.OnExit(e);
        }
    }
}