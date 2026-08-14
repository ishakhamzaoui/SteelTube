using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SteelTube.Application.Common;
using SteelTube.Domain.Exceptions;

namespace SteelTube.Desktop.Common
{
    /// <summary>
    /// Base class for every ViewModel. <see cref="RunAsync"/> centralizes
    /// the error-classification -> friendly-message translation from
    /// SAD 51/52: known Application/Domain exceptions surface their own
    /// message (they were already written to be user-facing), and anything
    /// unexpected becomes a generic message instead of a raw stack trace.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            protected set => SetProperty(ref _isBusy, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                    RaisePropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        private string _successMessage;
        public string SuccessMessage
        {
            get => _successMessage;
            private set
            {
                if (SetProperty(ref _successMessage, value))
                    RaisePropertyChanged(nameof(HasSuccessMessage));
            }
        }

        public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);

        protected void SetSuccessMessage(string message) => SuccessMessage = message;

        /// <summary>Runs an async operation with a busy flag and unified error handling.</summary>
        protected async Task RunAsync(Func<Task> action)
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
            try
            {
                await action();
            }
            catch (UseCaseException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (DomainException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                // Anything else is unexpected -- SAD 52: never show a raw
                // stack trace to a normal user. A future Logging phase
                // (SAD 53) is where this would also be written to disk.
                ErrorMessage = "Something went wrong. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}