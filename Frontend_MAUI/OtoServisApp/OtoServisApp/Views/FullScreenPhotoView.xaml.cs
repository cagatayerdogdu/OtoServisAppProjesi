using OtoServisApp.Models;

namespace OtoServisApp.Views;

public partial class FullScreenPhotoView : ContentPage
{
    double currentScale = 1;
    double startScale = 1;
    double xOffset = 0;
    double yOffset = 0;

    // KİLİT ÇÖZÜM 1: Çakışmayı önleyecek kilit değişkenimiz
    bool isPinching = false;

    public FullScreenPhotoView(List<ServisTalebiFotograf> fotolar, int baslangicIndeksi)
    {
        InitializeComponent();
        FotoCarousel.ItemsSource = fotolar;
        FotoCarousel.Position = baslangicIndeksi;
    }

    // --- ZOOM (YAKINLAŞTIRMA/UZAKLAŞTIRMA) MANTIĞI ---
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        // YENİ REVİZE: Anlık hesaplama hatalarının uygulamayı çökertmesini engellemek için Try-Catch bloğu eklendi.
        try
        {
            var resim = sender as Image;
            if (resim == null) return;

            if (e.Status == GestureStatus.Started)
            {
                FotoCarousel.IsSwipeEnabled = false;
                startScale = resim.Scale;

                // YENİ REVİZE: Saçmalamayı ve zıplamayı önlemek için Çapa (Anchor) noktasını her zaman tam merkeze sabitliyoruz.
                resim.AnchorX = 0.5;
                resim.AnchorY = 0.5;
            }
            else if (e.Status == GestureStatus.Running)
            {
                double targetScale = startScale * e.Scale;
                currentScale = Math.Max(1, Math.Min(targetScale, 4));

                resim.Scale = currentScale;
            }
            else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
            {
                if (currentScale <= 1.05)
                {
                    currentScale = 1;
                    resim.ScaleTo(1, 250, Easing.CubicInOut);
                    resim.TranslateTo(0, 0, 250, Easing.CubicInOut);
                    xOffset = 0;
                    yOffset = 0;
                    FotoCarousel.IsSwipeEnabled = true;
                }
            }
        }
        catch (Exception)
        {
            // Olası bir gesture çakışmasında uygulamanın kapanmasını engelliyoruz
        }
    }

    // --- PAN (YAKINLAŞINCA RESİM İÇİNDE GEZİNME) MANTIĞI ---
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        // YENİ REVİZE: Sürükleme esnasında kilitlenmeleri önlemek için Try-Catch bloğu eklendi.
        try
        {
            var resim = sender as Image;

            if (resim == null || currentScale <= 1.05) return;

            if (e.StatusType == GestureStatus.Started)
            {
                // Mevcut çalışan kod korundu: Başlangıç noktası belirleniyor
                xOffset = resim.TranslationX;
                yOffset = resim.TranslationY;
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                // YENİ REVİZE: Resmin ekran dışına sürüklenip Android motorunu çökertmesini engelleyen Sınır (Clamp) matematiği eklendi.
                double maxTranslationX = (resim.Width * currentScale - resim.Width) / 2;
                double maxTranslationY = (resim.Height * currentScale - resim.Height) / 2;

                double yeniX = xOffset + e.TotalX;
                double yeniY = yOffset + e.TotalY;

                // Resim sadece kendi sınırları içinde kaydırılabilir, dışarı çıkamaz
                resim.TranslationX = Math.Max(-maxTranslationX, Math.Min(yeniX, maxTranslationX));
                resim.TranslationY = Math.Max(-maxTranslationY, Math.Min(yeniY, maxTranslationY));
            }
            else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                xOffset = resim.TranslationX;
                yOffset = resim.TranslationY;
            }
        }
        catch (Exception)
        {
            // Olası bir koordinat tanımsızlığında uygulamanın çökmesini (VMDisconnected) engelliyoruz
        }
    }

    private async void OnKapatClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}