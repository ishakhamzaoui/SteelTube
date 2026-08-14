using System;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>
    /// One entry in the left-hand navigation (SAD 49). ViewModels are built
    /// on demand each time an item is selected, rather than kept alive in
    /// the background -- with SQLite as the source of truth there's no
    /// benefit to caching stale in-memory copies, and it keeps memory use
    /// down (SAD 58).
    /// </summary>
    public sealed class NavigationItem
    {
        public string Title { get; }
        public Func<object> CreateViewModel { get; }

        public NavigationItem(string title, Func<object> createViewModel)
        {
            Title = title;
            CreateViewModel = createViewModel;
        }
    }
}