namespace AbeGamingBlazorApp.Layout
{
    /// <summary>Layout settings that a page can change, observed by <see cref="MainLayout"/>.</summary>
    public sealed class LayoutState
    {
        private bool _compactNavOnLandscapePhone;

        /// <summary>Raised when any setting changes.</summary>
        public event Action? Changed;

        /// <summary>
        /// True while the page needs the full width of a phone held in landscape: the sidebar is
        /// then replaced by the narrow-screen top bar with its menu button.
        /// </summary>
        public bool CompactNavOnLandscapePhone
        {
            get => _compactNavOnLandscapePhone;
            set
            {
                if (_compactNavOnLandscapePhone == value)
                    return;
                _compactNavOnLandscapePhone = value;
                Changed?.Invoke();
            }
        }
    }
}
