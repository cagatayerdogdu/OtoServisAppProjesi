using OtoServisApp.Models;

namespace OtoServisApp.Views;

public partial class FullScreenPhotoView : ContentPage
{
    double currentScale = 1;
    double startScale = 1;
    double xOffset = 0;
    double yOffset = 0;

    public FullScreenPhotoView(List<ServisTalebiFotograf> fotolar, int baslangicIndeksi)
    {
        InitializeComponent();

        // Resim listesini Carousel'e bağlıyoruz
        FotoCarousel.ItemsSource = fotolar;

        // Tıklanan resimden başlamasını sağlıyoruz
        FotoCarousel.Position = baslangicIndeksi;
    }

    // --- ZOOM (YAKINLAŞTIRMA) MANTIĞI ---
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        var resim = sender as Image;
        if (resim == null) return;

        if (e.Status == GestureStatus.Started)
        {
            startScale = resim.Scale;
            resim.AnchorX = 0.5;
            resim.AnchorY = 0.5;
        }
        else if (e.Status == GestureStatus.Running)
        {
            // Ölçeği hesapla ve sınırla (Min: 1x, Max: 5x)
            currentScale += (e.Scale - 1) * startScale;
            currentScale = Math.Clamp(currentScale, 1, 5);

            resim.Scale = currentScale;

            // Resim büyüdüğünde kaydırmayı engellemek istersen Carousel'i kilitleyebilirsin
            FotoCarousel.IsSwipeEnabled = currentScale <= 1.1;
        }
        else if (e.Status == GestureStatus.Completed)
        {
            // Resim çok küçülürse orijinal boyuta döndür
            if (currentScale < 1.1)
            {
                resim.ScaleTo(1, 250, Easing.CubicOut);
                resim.TranslateTo(0, 0, 250, Easing.CubicOut);
                currentScale = 1;
                FotoCarousel.IsSwipeEnabled = true;
            }
        }
    }

    // --- PAN (YAKINLAŞINCA RESİM İÇİNDE GEZİNME) MANTIĞI ---
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var resim = sender as Image;
        if (resim == null || currentScale <= 1.1) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Sadece yakınlaşmışken resim içinde sağa sola gitmeye izin ver
                resim.TranslationX = xOffset + e.TotalX;
                resim.TranslationY = yOffset + e.TotalY;
                break;

            case GestureStatus.Completed:
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