using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;

namespace OtoServisApp.Controls
{
    public class PinchToZoomContainer : ContentView
    {
        public event Action<bool> ZoomStateChanged;

        double _currentScale = 1;
        double _startScale = 1;
        double _xOffset = 0;
        double _yOffset = 0;
        double _velocityX = 0;
        double _velocityY = 0;
        bool _isPanning = false;

        public PinchToZoomContainer()
        {
            var pinch = new PinchGestureRecognizer();
            pinch.PinchUpdated += OnPinchUpdated;

            var pan = new PanGestureRecognizer();
            pan.PanUpdated += OnPanUpdated;

            var tap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
            tap.Tapped += OnDoubleTapped;

            GestureRecognizers.Add(pinch);
            GestureRecognizers.Add(pan);
            GestureRecognizers.Add(tap);
        }

        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (Content == null) return;

            if (e.Status == GestureStatus.Started)
            {
                _startScale = _currentScale;
            }
            else if (e.Status == GestureStatus.Running)
            {
                double newScale = Math.Clamp(_startScale * e.Scale, 1, 4);
                double scaleFactor = newScale / _currentScale;
                _currentScale = newScale;

                // Eğer Content boyutları henüz hazır değilse işlemi atla
                if (Content.Width <= 0 || Content.Height <= 0) return;

                double originX = (e.ScaleOrigin.X - 0.5) * Content.Width;
                double originY = (e.ScaleOrigin.Y - 0.5) * Content.Height;

                double targetX = Content.TranslationX - originX * (scaleFactor - 1);
                double targetY = Content.TranslationY - originY * (scaleFactor - 1);

                var bounds = CalculateBounds();

                Content.Scale = _currentScale;
                Content.TranslationX = Clamp(targetX, bounds.minX, bounds.maxX);
                Content.TranslationY = Clamp(targetY, bounds.minY, bounds.maxY);

                ZoomStateChanged?.Invoke(_currentScale > 1.01);
            }
            else if (e.Status == GestureStatus.Completed)
            {
                if (_currentScale < 1.05)
                    Reset();
            }
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (Content == null || _currentScale <= 1) return;
            if (Content.Width <= 0 || Content.Height <= 0) return; // Boyutlar hazır değilse işlem yapma

            if (e.StatusType == GestureStatus.Started)
            {
                _isPanning = true;
                _velocityX = 0;
                _velocityY = 0;
                ZoomStateChanged?.Invoke(true);
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                double newX = _xOffset + e.TotalX;
                double newY = _yOffset + e.TotalY;

                var bounds = CalculateBounds();

                Content.TranslationX = Clamp(newX, bounds.minX, bounds.maxX);
                Content.TranslationY = Clamp(newY, bounds.minY, bounds.maxY);

                _velocityX = e.TotalX;
                _velocityY = e.TotalY;
            }
            else if (e.StatusType == GestureStatus.Completed)
            {
                _xOffset = Content.TranslationX;
                _yOffset = Content.TranslationY;
                _isPanning = false;
                StartInertia();
            }
        }

        private async void OnDoubleTapped(object sender, TappedEventArgs e)
        {
            if (Content == null) return;
            if (Content.Width <= 0 || Content.Height <= 0) return;

            var tapPoint = e.GetPosition(Content);
            if (tapPoint == null) return;

            if (_currentScale > 1)
            {
                Reset();
            }
            else
            {
                double newScale = 2;
                double originX = (tapPoint.Value.X / Content.Width - 0.5) * Content.Width;
                double originY = (tapPoint.Value.Y / Content.Height - 0.5) * Content.Height;

                _currentScale = newScale;

                await Content.ScaleTo(newScale, 200);

                Content.TranslationX = -originX;
                Content.TranslationY = -originY;

                _xOffset = Content.TranslationX;
                _yOffset = Content.TranslationY;

                ZoomStateChanged?.Invoke(true);
            }
        }

        private async void StartInertia()
        {
            while (Math.Abs(_velocityX) > 0.1 || Math.Abs(_velocityY) > 0.1)
            {
                _velocityX *= 0.9;
                _velocityY *= 0.9;

                double newX = Content.TranslationX + _velocityX;
                double newY = Content.TranslationY + _velocityY;

                var bounds = CalculateBounds();

                Content.TranslationX = Clamp(newX, bounds.minX, bounds.maxX);
                Content.TranslationY = Clamp(newY, bounds.minY, bounds.maxY);

                await Task.Delay(16);
            }
        }

        private async void Reset()
        {
            await Content.ScaleTo(1, 200);
            await Content.TranslateTo(0, 0, 200);

            _currentScale = 1;
            _xOffset = 0;
            _yOffset = 0;

            ZoomStateChanged?.Invoke(false);
        }

        private (double minX, double maxX, double minY, double maxY) CalculateBounds()
        {
            // ✅ BOYUT KONTROLÜ: Eğer Content boyutları henüz hazır değilse varsayılan değerler kullan
            double contentWidth = Content.Width > 0 ? Content.Width : 300;
            double contentHeight = Content.Height > 0 ? Content.Height : 300;
            double containerWidth = this.Width > 0 ? this.Width : 400;
            double containerHeight = this.Height > 0 ? this.Height : 800;

            double scaledWidth = contentWidth * _currentScale;
            double scaledHeight = contentHeight * _currentScale;

            double maxX = Math.Max(0, (scaledWidth - containerWidth) / 2);
            double maxY = Math.Max(0, (scaledHeight - containerHeight) / 2);

            return (-maxX, maxX, -maxY, maxY);
        }

        private double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}