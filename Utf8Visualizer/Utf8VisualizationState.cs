using System;
using System.Collections.Generic;

namespace Utf8Visualizer
{
    internal static class Utf8VisualizationState
    {
        private static bool _isGloballyEnabled = true;
        private static readonly Dictionary<string, bool> _perDocumentEnabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        /// <summary>
        /// Vyvolá se při změně globálního zapnutí/vypnutí vizualizace.
        /// </summary>
        public static event EventHandler IsEnabledChanged;

        /// <summary>
        /// Vyvolá se při změně libovolného stavu (globálního i per-document).
        /// </summary>
        public static event EventHandler StateChanged;

        /// <summary>
        /// Globální zapnutí/vypnutí vizualizace pro všechny dokumenty.
        /// </summary>
        public static bool IsGloballyEnabled
        {
            get => _isGloballyEnabled;
            set
            {
                if (_isGloballyEnabled == value)
                {
                    return;
                }

                _isGloballyEnabled = value;
                IsEnabledChanged?.Invoke(null, EventArgs.Empty);
                StateChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Zpětná kompatibilita – alias pro IsGloballyEnabled.
        /// </summary>
        public static bool IsEnabled
        {
            get => IsGloballyEnabled;
            set => IsGloballyEnabled = value;
        }

        /// <summary>
        /// Vrátí true, pokud je vizualizace zapnutá pro daný dokument (globálně i lokálně).
        /// </summary>
        public static bool IsEnabledForDocument(string filePath)
        {
            if (!_isGloballyEnabled)
            {
                return false;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                return true;
            }

            lock (_lock)
            {
                if (_perDocumentEnabled.TryGetValue(filePath, out var perDoc))
                {
                    return perDoc;
                }
            }

            return true;
        }

        /// <summary>
        /// Nastaví per-document stav. true = zapnuto, false = vypnuto, null = reset na default.
        /// </summary>
        public static void SetPerDocumentState(string filePath, bool? state)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            lock (_lock)
            {
                if (state.HasValue)
                {
                    _perDocumentEnabled[filePath] = state.Value;
                }
                else
                {
                    _perDocumentEnabled.Remove(filePath);
                }
            }

            StateChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Vrátí true, pokud je dokument explicitně vypnutý.
        /// </summary>
        public static bool IsDocumentExplicitlyDisabled(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            lock (_lock)
            {
                return _perDocumentEnabled.TryGetValue(filePath, out var perDoc) && !perDoc;
            }
        }
    }
}
