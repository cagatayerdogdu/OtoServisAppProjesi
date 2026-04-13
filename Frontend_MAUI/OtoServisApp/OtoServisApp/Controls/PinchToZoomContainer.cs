using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;

namespace OtoServisApp.Controls
{
    public class PinchToZoomContainer : ContentView
    {
        private double _currentScale = 1;
        private double _startScale = 1;
        private double _xOffset = 0;
        private double _yOffset = 0;

        public PinchToZoomContainer()
        {
            // Taşmayı engellemek için kritik ayar
            this.IsClippedToBounds = true;

            var pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;
            GestureRecognizers.Add(pinchGesture);

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            GestureRecognizers.Add(panGesture);

            var tapGesture = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
            tapGesture.Tapped += OnDoubleTapped;
            GestureRecognizers.Add(tapGesture);
        }

        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (Content == null) return;

            if (e.Status == GestureStatus.Started)
            {
                _startScale = Content.Scale;
            }
            else if (e.Status == GestureStatus.Running)
            {
                // Yakınlaştırma oranını hesapla (1 ile 4 kat arasında sınırla)
                _currentScale = Math.Clamp(_startScale * e.Scale, 1.0, 4.0);
                Content.Scale = _currentScale;

                // Yakınlaştırma yapılırken resmi ortala
                Content.TranslationX = 0;
                Content.TranslationY = 0;
                _xOffset = 0;
                _yOffset = 0;
            }
            else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
            {
                if (_currentScale <= 1.05)
                {
                    // Orijinal boyuta geri dön
                    this.AbortAnimation("Reset");
                    var resetAnimation = new Animation();

                    var scaleAnimation = new Animation(v => Content.Scale = v, Content.Scale, 1);
                    var translateXAnimation = new Animation(v => Content.TranslationX = v, Content.TranslationX, 0);
                    var translateYAnimation = new Animation(v => Content.TranslationY = v, Content.TranslationY, 0);

                    resetAnimation.Add(0, 1, scaleAnimation);
                    resetAnimation.Add(0, 1, translateXAnimation);
                    resetAnimation.Add(0, 1, translateYAnimation);

                    resetAnimation.Commit(this, "Reset", 16, 250, Easing.CubicInOut, (v, c) =>
                    {
                        _currentScale = 1;
                        _xOffset = 0;
                        _yOffset = 0;
                    });
                }
                else
                {
                    _xOffset = Content.TranslationX;
                    _yOffset = Content.TranslationY;
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

                // Görselin taşmasını engelleyen sınırları hesapla
                (double minX, double maxX, double minY, double maxY) = CalculateBounds();

                Content.TranslationX = Math.Clamp(newX, minX, maxX);
                Content.TranslationY = Math.Clamp(newY, minY, maxY);
            }
            else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                _xOffset = Content.TranslationX;
                _yOffset = Content.TranslationY;
            }
        }

        private async void OnDoubleTapped(object sender, TappedEventArgs e)
        {
            if (Content == null) return;

            if (_currentScale > 1)
            {
                // Zoom'u sıfırla
                await Content.ScaleTo(1, 250, Easing.CubicInOut);
                await Content.TranslateTo(0, 0, 250, Easing.CubicInOut);
                _currentScale = 1;
                _xOffset = 0;
                _yOffset = 0;
            }
            else
            {
                // 2 kat yakınlaştır
                _currentScale = 2;
                await Content.ScaleTo(2, 250, Easing.CubicInOut);
                _xOffset = 0;
                _yOffset = 0;
            }
        }

        private (double minX, double maxX, double minY, double maxY) CalculateBounds()
        {
            if (Content == null) return (0, 0, 0, 0);

            // İçeriğin ölçeklenmiş boyutlarını hesapla
            var contentWidth = Content.Width > 0 ? Content.Width : 300;
            var contentHeight = Content.Height > 0 ? Content.Height : 300;
            var scaledWidth = contentWidth * _currentScale;
            var scaledHeight = contentHeight * _currentScale;

            // Container'ın boyutları
            var containerWidth = this.Width > 0 ? this.Width : 400;
            var containerHeight = this.Height > 0 ? this.Height : 800;

            // İçerik container'dan büyükse, hareket alanını hesapla
            var maxOffsetX = Math.Max(0, (scaledWidth - containerWidth) / 2);
            var maxOffsetY = Math.Max(0, (scaledHeight - containerHeight) / 2);

            return (-maxOffsetX, maxOffsetX, -maxOffsetY, maxOffsetY);
        }
    }
}