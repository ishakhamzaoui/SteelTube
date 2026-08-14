using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SteelTube.Application.Backup.CreateBackup;
using SteelTube.Application.Backup.RestoreBackup;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>
    /// Backs the "Data" screen (SRS 9.2 / SRS 16). Uses WPF's file dialog
    /// and a confirmation prompt directly rather than routing them through
    /// an interface -- a small, pragmatic exception to strict MVVM
    /// purity that keeps this screen simple (SAD Goal 5, operational
    /// simplicity), since neither is meaningfully unit-testable business
    /// logic.
    /// </summary>
    public sealed class BackupViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;

        private string _lastBackupPath;
        public string LastBackupPath { get => _lastBackupPath; private set => SetProperty(ref _lastBackupPath, value); }

        private bool _restartRequired;
        public bool RestartRequired { get => _restartRequired; private set => SetProperty(ref _restartRequired, value); }

        public AsyncRelayCommand CreateBackupCommand { get; }
        public RelayCommand ChooseAndRestoreCommand { get; }
        public RelayCommand RestartNowCommand { get; }

        public BackupViewModel(CompositionRoot root)
        {
            _root = root;
            CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
            ChooseAndRestoreCommand = new RelayCommand(ChooseAndRestore, () => !RestartRequired);
            RestartNowCommand = new RelayCommand(RestartNow, () => RestartRequired);
        }

        private Task CreateBackupAsync() => RunAsync(async () =>
        {
            var result = await _root.CreateBackup.HandleAsync(new CreateBackupCommand());
            LastBackupPath = result.FilePath;
            SetSuccessMessage($"Backup created: {result.FilePath}");
        });

        private void ChooseAndRestore()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a SteelTube backup to restore",
                Filter = "SteelTube backup (*.db)|*.db|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != true)
                return;

            // SAD 50: dangerous actions require confirmation.
            var confirmed = MessageBox.Show(
                "Restoring will replace all current data with the selected backup. " +
                "A safety copy of your current data will be made first, and SteelTube will need to restart. Continue?",
                "Confirm restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;

            if (!confirmed)
                return;

            _ = RunAsync(async () =>
            {
                await _root.RestoreBackup.HandleAsync(new RestoreBackupCommand { BackupFilePath = dialog.FileName });
                RestartRequired = true;
                ChooseAndRestoreCommand.RaiseCanExecuteChanged();
                RestartNowCommand.RaiseCanExecuteChanged();
                SetSuccessMessage("Restore complete. Restart SteelTube to see the restored data.");
            });
        }

        private void RestartNow()
        {
            var executablePath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(executablePath);
            System.Windows.Application.Current.Shutdown();
        }
    }
}