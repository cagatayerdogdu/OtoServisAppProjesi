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
        var resim = sender as Image;
        if (resim == null) return;

        if (e.Status == GestureStatus.Started)
        {
            isPinching = true;
            // 1. KAVGA BİTİRİCİ: İki parmak ekrana değdiği an Carousel'i felç et ki Android kafayı yemesin.
            FotoCarousel.IsSwipeEnabled = false;

            startScale = resim.Scale;
        }
        else if (e.Status == GestureStatus.Running)
        {
            // 2. MATEMATİK DÜZELTMESİ: Toplama yerine çarpma kullanıyoruz. MAUI'de en stabil zoom budur.
            double targetScale = startScale * e.Scale;
            currentScale = Math.Max(1, Math.Min(targetScale, 4)); // 1x ile 4x arası sınır

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
                FotoCarousel.IsSwipeEnabled = true; // Sadece orijinal boyuttayken sağa sola geçiş serbest
            }

            isPinching = false;
        }
    }

    // --- PAN (YAKINLAŞINCA RESİM İÇİNDE GEZİNME) MANTIĞI ---
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var resim = sender as Image;

        // Eğer iki parmak ekrandaysa (zoom yapılıyorsa) veya resim küçükse SÜZGEÇTEN GEÇEMEZ.
        if (resim == null || isPinching || currentScale <= 1.05) return;

        if (e.StatusType == GestureStatus.Started)
        {
            // 3. ZIPLAMA (TELEPORT) ÇÖZÜMÜ: Parmağını ekrana her dokundurduğunda, 
            // resmin O ANKİ konumunu başlangıç noktası kabul et. (Bunu eklemediğimiz için zıplıyordu).
            xOffset = resim.TranslationX;
            yOffset = resim.TranslationY;
        }
        else if (e.StatusType == GestureStatus.Running)
        {
            // Parmağı kaydırdıkça o anki konumun üzerine ekle
            resim.TranslationX = xOffset + e.TotalX;
            resim.TranslationY = yOffset + e.TotalY;
        }
        else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
        {
            // Parmağı çektiğinde son durumu hafızaya yaz
            xOffset = resim.TranslationX;
            yOffset = resim.TranslationY;
        }
    }

    private async void OnKapatClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}