using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SteelTube.Desktop.Common
{
    /// <summary>
    /// An ICommand for async operations (submitting a form, loading a
    /// list). Guards against re-entrancy: while the underlying task is
    /// running, CanExecute returns false so a second click can't fire a
    /// second overlapping submit (SAD 50 — dangerous/duplicate actions
    /// should not be easy to trigger twice).
    /// </summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object parameter)
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}