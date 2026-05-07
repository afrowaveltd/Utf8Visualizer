using System;

namespace Utf8Visualizer
{
    internal static class Utf8VisualizationState
    {
        private static bool _isEnabled = true;

        public static event EventHandler IsEnabledChanged;

        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                {
                    return;
                }

                _isEnabled = value;
                IsEnabledChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }
}
