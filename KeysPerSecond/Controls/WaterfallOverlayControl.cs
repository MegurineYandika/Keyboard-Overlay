using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using KeysPerSecond.Models;
using KeysPerSecond.Services;
using KeysPerSecond.ViewModels;

namespace KeysPerSecond.Controls
{
    /// <summary>
    /// Ultra-low-latency, zero-allocation custom FrameworkElement for the Real-time Piano Roll / Waterfall Bar Visualizer.
    /// Overrides OnRender to draw scrolling note bars directly on the render thread with pre-cached frozen brushes.
    /// </summary>
    public sealed class WaterfallOverlayControl : FrameworkElement
    {
        private struct ColumnMetrics
        {
            public double X;
            public double Width;
            public Brush Brush;
        }

        private AppConfig? _config;
        private NoteStreamBuffer? _buffer;
        private IReadOnlyList<KeyItemViewModel>? _trackedKeys;

        private ColumnMetrics[] _columns = Array.Empty<ColumnMetrics>();
        private Brush? _laneBackgroundBrush;

        private bool _isEnabled = true;
        private bool _isUpscroll = true;
        private bool _isEditMode = false;
        private double _scrollSpeed = 0.8;
        private double _pixelsPerTick = 0.0;
        private double _cornerRadius = 4.0;
        private double _maxVisibleDurationMs = 1500.0;
        private long _maxVisibleDurationTicks = 0;

        public WaterfallOverlayControl()
        {
            ClipToBounds = true;
            IsHitTestVisible = false;
        }

        /// <summary>
        /// Configures the control dependencies.
        /// </summary>
        public void Configure(AppConfig config, NoteStreamBuffer buffer, IReadOnlyList<KeyItemViewModel> trackedKeys)
        {
            _config = config;
            _buffer = buffer;
            _trackedKeys = trackedKeys;

            UpdateMetrics();
        }

        /// <summary>
        /// Updates Edit Mode status. When in Edit Mode, the waterfall is hidden and paused.
        /// </summary>
        public void SetEditMode(bool isEditMode)
        {
            if (_isEditMode != isEditMode)
            {
                _isEditMode = isEditMode;
                if (_isEditMode)
                {
                    _buffer?.Clear();
                }
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Recomputes column layout coordinates, scroll coefficients, and pre-allocates frozen brushes.
        /// Call whenever config, tracked keys, or layout dimensions change.
        /// Zero allocations occur after this update step.
        /// </summary>
        public void UpdateMetrics()
        {
            if (_config == null)
                return;

            var wf = _config.Waterfall;
            _isEnabled = wf.IsEnabled;
            _isUpscroll = !string.Equals(wf.ScrollDirection, "Downscroll", StringComparison.OrdinalIgnoreCase);
            _scrollSpeed = Math.Max(0.05, wf.ScrollSpeed);
            _cornerRadius = Math.Max(0.0, wf.BarCornerRadius);
            _maxVisibleDurationMs = Math.Max(100.0, wf.MaxVisibleDurationMs);

            _pixelsPerTick = (_scrollSpeed * 1000.0) / Stopwatch.Frequency;
            _maxVisibleDurationTicks = (long)(_maxVisibleDurationMs * Stopwatch.Frequency / 1000.0);

            // Pre-cache lane background brush (subtle translucent guide)
            var laneBg = new SolidColorBrush(Color.FromArgb(16, 255, 255, 255));
            laneBg.Freeze();
            _laneBackgroundBrush = laneBg;

            int keyCount = _trackedKeys?.Count ?? _config.TrackedKeys?.Count ?? 0;
            if (keyCount == 0)
            {
                _columns = Array.Empty<ColumnMetrics>();
                InvalidateVisual();
                return;
            }

            if (_columns.Length != keyCount)
            {
                _columns = new ColumnMetrics[keyCount];
            }

            double currentX = 0;
            byte alpha = (byte)(Math.Clamp(wf.BarOpacity, 0.05, 1.0) * 255.0);
            Color fallbackColor = Color.FromRgb(0x00, 0xE5, 0xFF);

            for (int i = 0; i < keyCount; i++)
            {
                double keyWidth = _config.Style.KeyWidth;
                string accentHex = _config.Style.AccentColor;

                if (_trackedKeys != null && i < _trackedKeys.Count)
                {
                    keyWidth = _trackedKeys[i].Width;
                    if (wf.UseKeyAccentColors)
                    {
                        accentHex = _trackedKeys[i].AccentColorHex;
                    }
                }
                else if (_config.TrackedKeys != null && i < _config.TrackedKeys.Count)
                {
                    keyWidth = _config.TrackedKeys[i].Width ?? _config.Style.KeyWidth;
                    if (wf.UseKeyAccentColors)
                    {
                        accentHex = _config.TrackedKeys[i].AccentColor ?? _config.Style.AccentColor;
                    }
                }

                Color baseColor = ParseColor(accentHex, fallbackColor);
                Color noteColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

                var brush = new SolidColorBrush(noteColor);
                brush.Freeze();

                // Key cards in MainWindow.xaml have Margin="4", so X starts at currentX + 4
                _columns[i] = new ColumnMetrics
                {
                    X = currentX + 4.0,
                    Width = Math.Max(10.0, keyWidth),
                    Brush = brush
                };

                currentX += keyWidth + 8.0;
            }

            // Sync control width with the total keys width
            this.Width = Math.Max(40.0, currentX);
            this.Height = Math.Max(40.0, wf.Height);

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (!_isEnabled || _isEditMode || _buffer == null || _columns.Length == 0)
                return;

            double actualHeight = ActualHeight;
            double actualWidth = ActualWidth;
            if (actualHeight <= 1.0 || actualWidth <= 1.0)
                return;

            long now = Stopwatch.GetTimestamp();

            // Cull offscreen notes past visible horizon
            _buffer.CullExpiredNotes(now, _maxVisibleDurationTicks);

            // Render subtle lane tracks
            if (_laneBackgroundBrush != null)
            {
                for (int col = 0; col < _columns.Length; col++)
                {
                    ref readonly ColumnMetrics cm = ref _columns[col];
                    if (_cornerRadius > 0)
                    {
                        dc.DrawRoundedRectangle(_laneBackgroundBrush, null, new Rect(cm.X, 0, cm.Width, actualHeight), _cornerRadius, _cornerRadius);
                    }
                    else
                    {
                        dc.DrawRectangle(_laneBackgroundBrush, null, new Rect(cm.X, 0, cm.Width, actualHeight));
                    }
                }
            }

            // Render completed notes (scrolling past)
            int completedCount = _buffer.CompletedCount;
            for (int i = 0; i < completedCount; i++)
            {
                ref readonly NoteSegment note = ref _buffer.GetCompletedNote(i);
                RenderNote(dc, in note, now, actualHeight);
            }

            // Render active held notes (stretching continuously from baseline)
            for (int col = 0; col < _columns.Length; col++)
            {
                if (_buffer.TryGetActiveNote(col, out NoteSegment activeNote))
                {
                    RenderNote(dc, in activeNote, now, actualHeight);
                }
            }
        }

        private void RenderNote(DrawingContext dc, in NoteSegment note, long now, double actualHeight)
        {
            int col = note.ColumnIndex;
            if ((uint)col >= (uint)_columns.Length)
                return;

            ref readonly ColumnMetrics colMetrics = ref _columns[col];
            Brush brush = colMetrics.Brush;
            double x = colMetrics.X;
            double width = colMetrics.Width;

            if (_isUpscroll)
            {
                // Upscroll: notes spawn at the bottom (Y = actualHeight) and scroll UP towards Y = 0
                double yBase = actualHeight;
                long headElapsedTicks = Math.Max(0, now - note.StartTimestamp);
                double headDistance = headElapsedTicks * _pixelsPerTick;
                double yHead = yBase - headDistance;

                double yTail;
                if (note.IsActive)
                {
                    // While held, note tail remains anchored at baseline
                    yTail = yBase;
                }
                else
                {
                    long tailElapsedTicks = Math.Max(0, now - note.EndTimestamp);
                    double tailDistance = tailElapsedTicks * _pixelsPerTick;
                    yTail = yBase - tailDistance;
                }

                // Offscreen culling check
                if (yTail <= 0 || yHead >= yBase)
                    return;

                double top = Math.Max(0, yHead);
                double bottom = Math.Min(yBase, yTail);
                double barHeight = bottom - top;

                if (barHeight <= 0.5)
                    return;

                var rect = new Rect(x, top, width, barHeight);
                if (_cornerRadius > 0)
                {
                    dc.DrawRoundedRectangle(brush, null, rect, _cornerRadius, _cornerRadius);
                }
                else
                {
                    dc.DrawRectangle(brush, null, rect);
                }
            }
            else
            {
                // Downscroll: notes spawn at the top (Y = 0) and scroll DOWN towards Y = actualHeight
                double yBase = 0.0;
                long headElapsedTicks = Math.Max(0, now - note.StartTimestamp);
                double headDistance = headElapsedTicks * _pixelsPerTick;
                double yHead = yBase + headDistance;

                double yTail;
                if (note.IsActive)
                {
                    yTail = yBase;
                }
                else
                {
                    long tailElapsedTicks = Math.Max(0, now - note.EndTimestamp);
                    double tailDistance = tailElapsedTicks * _pixelsPerTick;
                    yTail = yBase + tailDistance;
                }

                // Offscreen culling check
                if (yHead <= 0 || yTail >= actualHeight)
                    return;

                double top = Math.Max(0, yTail);
                double bottom = Math.Min(actualHeight, yHead);
                double barHeight = bottom - top;

                if (barHeight <= 0.5)
                    return;

                var rect = new Rect(x, top, width, barHeight);
                if (_cornerRadius > 0)
                {
                    dc.DrawRoundedRectangle(brush, null, rect, _cornerRadius, _cornerRadius);
                }
                else
                {
                    dc.DrawRectangle(brush, null, rect);
                }
            }
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return fallback;

            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return fallback;
            }
        }
    }
}
