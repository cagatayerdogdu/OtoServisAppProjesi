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
            // Yakınlaştırma başladı, Pan (sürükleme) hareketini YASAKLA!
            isPinching = true;

            startScale = resim.Scale;
            resim.AnchorX = e.ScaleOrigin.X;
            resim.AnchorY = e.ScaleOrigin.Y;
        }
        else if (e.Status == GestureStatus.Running)
        {
            currentScale += (e.Scale - 1) * startScale;
            currentScale = Math.Clamp(currentScale, 1, 5);

            resim.Scale = currentScale;

            FotoCarousel.IsSwipeEnabled = currentScale == 1;
        }
        else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            if (currentScale <= 1.05)
            {
                currentScale = 1;
                resim.ScaleTo(1, 250, Easing.SpringOut);
                resim.TranslateTo(0, 0, 250, Easing.SpringOut);
                xOffset = 0;
                yOffset = 0;
                FotoCarousel.IsSwipeEnabled = true;
            }

            // Yakınlaştırma işlemi tamamen bitti, sürüklemeye tekrar izin verilebilir
            isPinching = false;
        }
    }

    // --- PAN (YAKINLAŞINCA RESİM İÇİNDE GEZİNME) MANTIĞI ---
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var resim = sender as Image;

        // KİLİT ÇÖZÜM 2: Eğer zoom yapılıyorsa (isPinching) VEYA resim orijinal boyuttaysa,
        // sürükleme hareketlerini tamamen ve anında İPTAL ET. 
        if (resim == null || isPinching || currentScale <= 1.05) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Sadece resim gerçekten büyümüşse ve zoom yapılmıyorsa gezinebilir
                resim.TranslationX = xOffset + e.TotalX;
                resim.TranslationY = yOffset + e.TotalY;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                xOffset = resim.TranslationX;
                yOffset = resim.TranslationY;
                break;
        }
    }

    private async void OnKapatClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}