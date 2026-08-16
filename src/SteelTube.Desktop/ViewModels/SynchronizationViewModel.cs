using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SteelTube.Application.Synchronization.ApplyImport;
using SteelTube.Application.Synchronization.Export;
using SteelTube.Application.Synchronization.PreviewImport;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>
    /// Backs the "Synchronize" screen (SRS 9.2 Data > Synchronize; SAD 40
    /// Import Preview). Picking a file loads a read-only preview
    /// immediately; nothing is written until Apply is confirmed.
    /// </summary>
    public sealed class SynchronizationViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;
        private string _pendingPackageJson;

        private string _selectedFileName;
        public string SelectedFileName { get => _selectedFileName; private set => SetProperty(ref _selectedFileName, value); }

        private PreviewImportResult _preview;
        public PreviewImportResult Preview { get => _preview; private set => SetProperty(ref _preview, value); }

        public bool HasPreview => Preview != null;

        public ObservableCollection<AffectedMaterialPreview> AffectedMaterials { get; } = new ObservableCollection<AffectedMaterialPreview>();

        private ApplyImportResult _lastApplyResult;
        public ApplyImportResult LastApplyResult { get => _lastApplyResult; private set => SetProperty(ref _lastApplyResult, value); }

        public AsyncRelayCommand ExportCommand { get; }
        public RelayCommand ChooseFileCommand { get; }
        public AsyncRelayCommand ApplyCommand { get; }

        public SynchronizationViewModel(CompositionRoot root)
        {
            _root = root;
            ExportCommand = new AsyncRelayCommand(ExportAsync);
            ChooseFileCommand = new RelayCommand(ChooseFile);
            ApplyCommand = new AsyncRelayCommand(ApplyAsync, () => HasPreview && Preview.NewOperationsCount > 0);
        }

        private Task ExportAsync() => RunAsync(async () =>
        {
            var result = await _root.ExportOperations.HandleAsync(new ExportOperationsCommand());

            var dialog = new SaveFileDialog
            {
                Title = "Save synchronization file",
                FileName = result.SuggestedFileName,
                Filter = "SteelTube synchronization file (*.json)|*.json"
            };
            if (dialog.ShowDialog() != true)
                return;

            File.WriteAllText(dialog.FileName, result.PackageJson);
            SetSuccessMessage($"Exported {result.OperationCount} operation(s) to {dialog.FileName}.");
        });

        private void ChooseFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a SteelTube synchronization file",
                Filter = "SteelTube synchronization file (*.json)|*.json|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != true)
                return;

            SelectedFileName = dialog.FileName;
            LastApplyResult = null;

            _ = RunAsync(async () =>
            {
                _pendingPackageJson = File.ReadAllText(dialog.FileName);
                var preview = await _root.PreviewImport.HandleAsync(new PreviewImportQuery { PackageJson = _pendingPackageJson });

                Preview = preview;
                RaisePropertyChanged(nameof(HasPreview));
                AffectedMaterials.Clear();
                foreach (var material in preview.AffectedMaterials)
                    AffectedMaterials.Add(material);

                ApplyCommand.RaiseCanExecuteChanged();
            });
        }

        private Task ApplyAsync() => RunAsync(async () =>
        {
            // SAD 50: dangerous/impactful actions require confirmation.
            var confirmed = MessageBox.Show(
                $"Apply {Preview.NewOperationsCount} new operation(s) from \"{Preview.SourceDeviceName}\"? " +
                "A safety backup of your current data will be made first.",
                "Confirm synchronization",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (!confirmed)
                return;

            var result = await _root.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = _pendingPackageJson });

            LastApplyResult = result;
            var message = $"Applied {result.NewOperationsInserted} new operation(s); {result.AlreadyKnownSkipped} were already known.";
            if (result.NegativeStockWarnings.Count > 0)
                message += $" {result.NegativeStockWarnings.Count} material(s) now show negative stock -- please review (SAD \u00a737/38).";
            SetSuccessMessage(message);

            _pendingPackageJson = null;
            Preview = null;
            RaisePropertyChanged(nameof(HasPreview));
            AffectedMaterials.Clear();
            SelectedFileName = null;
            ApplyCommand.RaiseCanExecuteChanged();
        });
    }
}