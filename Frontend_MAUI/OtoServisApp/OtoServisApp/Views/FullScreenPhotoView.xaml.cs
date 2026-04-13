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
            startScale = resim.Scale;

            // DONMA ÇÖZÜMÜ: Saniyede 60 kez yerine, sadece hareket başladığında Carousel'i kilitliyoruz.
            FotoCarousel.IsSwipeEnabled = false;

            // SAÇMALAMA ÇÖZÜMÜ: Sadece resim 1x (orijinal) boyutundayken çapa noktasını değiştir!
            // Eğer resim zaten büyümüşse ve parmak kaldırıp tekrar konduysa, çapa değişimi resmi zıplatır.
            if (currentScale <= 1.05)
            {
                resim.AnchorX = e.ScaleOrigin.X;
                resim.AnchorY = e.ScaleOrigin.Y;
            }
        }
        else if (e.Status == GestureStatus.Running)
        {
            currentScale += (e.Scale - 1) * startScale;
            currentScale = Math.Max(1, Math.Min(currentScale, 5));

            resim.Scale = currentScale;
            // DİKKAT: Burada IsSwipeEnabled = ... kodu vardı, donmayı engellemek için SİLDİK.
        }
        else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            if (currentScale <= 1.05)
            {
                // Resim küçüldüyse orijinal hale döndür ve Carousel'i tekrar aktif et
                currentScale = 1;
                resim.ScaleTo(1, 250, Easing.SpringOut);
                resim.TranslateTo(0, 0, 250, Easing.SpringOut);
                xOffset = 0;
                yOffset = 0;
                FotoCarousel.IsSwipeEnabled = true;
            }
            else
            {
                // Resim hala büyükse, başka fotoğrafa kaymamak için Carousel kilitli kalsın
                FotoCarousel.IsSwipeEnabled = false;
            }

            isPinching = false;
        }
    }

    // --- PAN (YAKINLAŞINCA RESİM İÇİNDE GEZİNME) MANTIĞI ---
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var resim = sender as Image;

        if (resim == null || isPinching || currentScale <= 1.05) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Parmak kaldırıp tekrar sürüklendiğinde bıraktığı yerden devam etmesi için xOffset kullanıyoruz
                resim.TranslationX = xOffset + e.TotalX;
                resim.TranslationY = yOffset + e.TotalY;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                // Sürükleme bittiğinde son koordinatları hafızaya al
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