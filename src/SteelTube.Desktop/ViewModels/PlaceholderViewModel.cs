using SteelTube.Desktop.Common;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>Stand-in for screens whose backing use cases aren't built yet (SAD 73 Phase 6/7).</summary>
    public sealed class PlaceholderViewModel : ViewModelBase
    {
        public string Title { get; }
        public string Message { get; }

        public PlaceholderViewModel(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
}