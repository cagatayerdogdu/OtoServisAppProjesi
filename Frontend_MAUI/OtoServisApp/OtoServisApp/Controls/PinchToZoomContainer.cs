using Microsoft.Maui.Controls;
using System;

namespace OtoServisApp.Controls
{
    public class PinchToZoomContainer : ContentView
    {
        public event Action<bool> ZoomStateChanged;

        double _currentScale = 1;
        double _startScale = 1;
        double _xOffset = 0;
        double _yOffset = 0;

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

                // 1. KİLİT ÇÖZÜM: Parmak ekrana değdiği an Carousel'i felç et ki hareketi ÇALMASIN!
                // Diğer kodda bu unutulduğu için yakınlaştırma hiç çalışmıyordu.
                ZoomStateChanged?.Invoke(true);
            }
            else if (e.Status == GestureStatus.Running)
            {
                // Basit ve stabil büyüme matematiği
                double targetScale = _startScale * e.Scale;
                _currentScale = Math.Clamp(targetScale, 1, 4);

                Content.Scale = _currentScale;
            }
            else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
            {
                if (_currentScale <= 1.05)
                {
                    ResetToNormal();
                }
                else
                {
                    // Resim hala büyükse Carousel kilitli kalmaya devam etsin
                    ZoomStateChanged?.Invoke(true);
                }
            }
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (Content == null || _currentScale <= 1.05) return;

            if (e.StatusType == GestureStatus.Started)
            {
                _xOffset = Content.TranslationX;
                _yOffset = Content.TranslationY;
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                double newX = _xOffset + e.TotalX;
                double newY = _yOffset + e.TotalY;

                // 2. KİLİT ÇÖZÜM: Resmin dışarı kaçıp uygulamayı çökertmesini engelleyen Sınır (Clamp) matematiği
                double maxTranslationX = (Content.Width * _currentScale - Content.Width) / 2;
                double maxTranslationY = (Content.Height * _currentScale - Content.Height) / 2;

                Content.TranslationX = Math.Clamp(newX, -maxTranslationX, maxTranslationX);
                Content.TranslationY = Math.Clamp(newY, -maxTranslationY, maxTranslationY);
            }
            else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                _xOffset = Content.TranslationX;
                _yOffset = Content.TranslationY;
            }
        }

        private void OnDoubleTapped(object sender, TappedEventArgs e)
        {
            if (Content == null) return;

            if (_currentScale > 1.05)
            {
                ResetToNormal();
            }
            else
            {
                // Çift tıklayınca otomatik 2.5x büyüt
                _currentScale = 2.5;
                Content.ScaleTo(_currentScale, 250, Easing.CubicInOut);
                ZoomStateChanged?.Invoke(true);
            }
        }

        private void ResetToNormal()
        {
            _currentScale = 1;
            _xOffset = 0;
            _yOffset = 0;

            Content.ScaleTo(1, 250, Easing.CubicInOut);
            Content.TranslateTo(0, 0, 250, Easing.CubicInOut);

            // İşlem bitti, Carousel sağa sola kaymaya açılabilir
            ZoomStateChanged?.Invoke(false);
        }
    }
}