using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using SteelTube.Application.Diagnostics.CheckIntegrity;
using SteelTube.Application.Diagnostics.RepairProjection;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>SAD 65 -- the diagnostic screen for Database, Inventory projection, and Catalogue status.</summary>
    public sealed class DiagnosticsViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;

        private CheckIntegrityResult _result;
        public CheckIntegrityResult Result { get => _result; private set => SetProperty(ref _result, value); }

        public bool HasResult => Result != null;
        public bool HasMismatches => Result != null && Result.ProjectionMismatches.Count > 0;

        public ObservableCollection<ProjectionMismatch> Mismatches { get; } = new ObservableCollection<ProjectionMismatch>();

        public AsyncRelayCommand RunCheckCommand { get; }
        public AsyncRelayCommand RepairCommand { get; }

        public DiagnosticsViewModel(CompositionRoot root)
        {
            _root = root;
            RunCheckCommand = new AsyncRelayCommand(RunCheckAsync);
            RepairCommand = new AsyncRelayCommand(RepairAsync, () => HasMismatches);
            _ = RunCheckAsync();
        }

        private Task RunCheckAsync() => RunAsync(async () =>
        {
            var result = await _root.CheckIntegrity.HandleAsync(new CheckIntegrityQuery());

            Result = result;
            RaisePropertyChanged(nameof(HasResult));
            RaisePropertyChanged(nameof(HasMismatches));

            Mismatches.Clear();
            foreach (var mismatch in result.ProjectionMismatches)
                Mismatches.Add(mismatch);

            RepairCommand.RaiseCanExecuteChanged();

            if (result.IsFullyHealthy)
                SetSuccessMessage("Everything checks out.");
        });

        private Task RepairAsync() => RunAsync(async () =>
        {
            // SAD 50: confirm before an action that rewrites data, even a corrective one.
            var confirmed = MessageBox.Show(
                "This recalculates every material's stock from its full operation history and overwrites the current totals. Continue?",
                "Confirm repair",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;

            if (!confirmed)
                return;

            await _root.RepairProjection.HandleAsync(new RepairProjectionCommand());
            await RunCheckAsync();
        });
    }
}