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

    // --- ZOOM (YAKINLAŞTIRMA/UZAKLAŞTIRMA) MANTIĞI ---
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        var resim = sender as Image;
        if (resim == null) return;

        if (e.Status == GestureStatus.Started)
        {
            startScale = resim.Scale;

            // KİLİT REVİZE 1: Yakınlaşma merkezini (Çapa noktasını) parmaklarının ortası olarak belirliyoruz.
            // Böylece resmin neresini çimdiklersen orası büyür.
            resim.AnchorX = e.ScaleOrigin.X;
            resim.AnchorY = e.ScaleOrigin.Y;
        }
        else if (e.Status == GestureStatus.Running)
        {
            // Büyüme oranını hesapla ve sınırları 1 ile 5 arasında tut
            currentScale += (e.Scale - 1) * startScale;
            currentScale = Math.Max(1, Math.Min(currentScale, 5));

            resim.Scale = currentScale;

            // Resim orijinal boyutta değilse Carousel'in (sağa sola kaydırma) hareketini kilitle
            FotoCarousel.IsSwipeEnabled = currentScale == 1;
        }
        else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            // KİLİT REVİZE 2: Parmaklarını ekrandan çektiğin anda resim 1x'e yakınsa,
            // titreme yapmadan, yumuşak bir yaylanma efektiyle (SpringOut) eski yerine oturt.
            if (currentScale <= 1.05)
            {
                currentScale = 1;
                // Easing.SpringOut efekti ile yerine çok tatlı bir şekilde oturur
                resim.ScaleTo(1, 300, Easing.SpringOut);
                resim.TranslateTo(0, 0, 300, Easing.SpringOut);
                xOffset = 0;
                yOffset = 0;
                FotoCarousel.IsSwipeEnabled = true;
            }
        }
    }

    // --- PAN (YAKINLAŞINCA RESİM İÇİNDE GEZİNME) MANTIĞI ---
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var resim = sender as Image;

        // Resim 1.05'ten küçükse (orijinal boyuttaysa) kaydırmayı tamamen iptal et
        if (resim == null || currentScale <= 1.05) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Parmakla resmi sürükleme
                resim.TranslationX = xOffset + e.TotalX;
                resim.TranslationY = yOffset + e.TotalY;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                // Sürükleme bitince son konumu hafızaya al
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