using Microsoft.Maui.Controls;
using System;

namespace OtoServisApp.Controls
{
    public class PinchToZoomContainer : ContentView
    {
        public event Action<bool> ZoomStateChanged;

        double _currentScale = 1;
        double _xOffset = 0;
        double _yOffset = 0;

        public PinchToZoomContainer()
        {
            // Çift Tıklama (Double Tap)
            var tap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
            tap.Tapped += OnDoubleTapped;

            // Kaydırma (Pan) - Sadece zoom yapılmışken çalışır
            var pan = new PanGestureRecognizer();
            pan.PanUpdated += OnPanUpdated;

            GestureRecognizers.Add(tap);
            GestureRecognizers.Add(pan);
        }

        private void OnDoubleTapped(object sender, TappedEventArgs e)
        {
            if (Content == null) return;

            if (_currentScale > 1)
            {
                ResetToNormal();
            }
            else
            {
                // Yakınlaştır
                _currentScale = 2.5;
                Content.ScaleTo(_currentScale, 250, Easing.CubicInOut);
                ZoomStateChanged?.Invoke(true); // Carousel'i kilitle
            }
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (Content == null || _currentScale <= 1.1) return;

            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    // Resmin dışarı kaçmasını engelleyen basit sınır kontrolü
                    double maxTranslationX = (Content.Width * _currentScale - Content.Width) / 2;
                    double maxTranslationY = (Content.Height * _currentScale - Content.Height) / 2;

                    Content.TranslationX = Math.Clamp(_xOffset + e.TotalX, -maxTranslationX, maxTranslationX);
                    Content.TranslationY = Math.Clamp(_yOffset + e.TotalY, -maxTranslationY, maxTranslationY);
                    break;

                case GestureStatus.Completed:
                    _xOffset = Content.TranslationX;
                    _yOffset = Content.TranslationY;
                    break;
            }
        }

        public void ResetToNormal()
        {
            _currentScale = 1;
            _xOffset = 0;
            _yOffset = 0;

            Content.ScaleTo(1, 250, Easing.CubicInOut);
            Content.TranslateTo(0, 0, 250, Easing.CubicInOut);
            ZoomStateChanged?.Invoke(false); // Carousel kilidini aç
        }
    }
}