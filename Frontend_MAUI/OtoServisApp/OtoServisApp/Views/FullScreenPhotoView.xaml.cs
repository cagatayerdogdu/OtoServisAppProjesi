using OtoServisApp.Models;

namespace OtoServisApp.Views;

public partial class FullScreenPhotoView : ContentPage
{
    double currentScale = 1;
    double startScale = 1;
    double xOffset = 0;
    double yOffset = 0;

    bool isPinching = false;

    public FullScreenPhotoView(List<ServisTalebiFotograf> fotolar, int baslangicIndeksi)
    {
        InitializeComponent();
        FotoCarousel.ItemsSource = fotolar;
        FotoCarousel.Position = baslangicIndeksi;
    }

    // --- ZOOM MANTIĞI (İYİLEŞTİRİLDİ) ---
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        var resim = sender as Image;
        if (resim == null) return;

        if (e.Status == GestureStatus.Started)
        {
            isPinching = true;
            FotoCarousel.IsSwipeEnabled = false;

            startScale = resim.Scale;
        }
        else if (e.Status == GestureStatus.Running)
        {
            // Matematiksel stabilite için çarpma kullanıyoruz
            double targetScale = startScale * e.Scale;
            currentScale = Math.Clamp(targetScale, 1.0, 4.0); // .NET 6+ için Math.Clamp

            resim.Scale = currentScale;

            // Zoom yapılırken pan offset'lerini sıfırla ki resim ekranda merkezde dursun
            resim.TranslationX = 0;
            resim.TranslationY = 0;
            xOffset = 0;
            yOffset = 0;
        }
        else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            if (currentScale <= 1.05)
            {
                // Orijinal boyuta dön
                currentScale = 1;
                resim.ScaleTo(1, 250, Easing.CubicInOut);
                resim.TranslateTo(0, 0, 250, Easing.CubicInOut);
                xOffset = 0;
                yOffset = 0;
                FotoCarousel.IsSwipeEnabled = true;
            }
            else
            {
                // Zoom bittiğinde mevcut konumu hafızaya al
                xOffset = resim.TranslationX;
                yOffset = resim.TranslationY;
            }

            isPinching = false;
        }
    }

    // --- PAN MANTIĞI (SINIRLANDIRMA EKLENDİ) ---
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var resim = sender as Image;
        if (resim == null || isPinching || currentScale <= 1.05) return;

        if (e.StatusType == GestureStatus.Started)
        {
            // Zıplamayı önlemek için başlangıç konumunu sakla
            xOffset = resim.TranslationX;
            yOffset = resim.TranslationY;
        }
        else if (e.StatusType == GestureStatus.Running)
        {
            // Yeni konum hesapla
            double newX = xOffset + e.TotalX;
            double newY = yOffset + e.TotalY;

            // ÇÖZÜM 2: Resmin sınırlarını hesaplayıp taşmayı engelle
            (double minX, double maxX, double minY, double maxY) = HesaplaSinirlar(resim);

            newX = Math.Clamp(newX, minX, maxX);
            newY = Math.Clamp(newY, minY, maxY);

            resim.TranslationX = newX;
            resim.TranslationY = newY;
        }
        else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
        {
            xOffset = resim.TranslationX;
            yOffset = resim.TranslationY;
        }
    }

    // --- SINIR HESAPLAMA METODU ---
    private (double minX, double maxX, double minY, double maxY) HesaplaSinirlar(Image resim)
    {
        // Görselin gerçek boyutları (piksel)
        double imgWidth = resim.Width > 0 ? resim.Width : 300;   // Varsayılan değer
        double imgHeight = resim.Height > 0 ? resim.Height : 300;

        // Scale sonrası boyutlar
        double scaledWidth = imgWidth * currentScale;
        double scaledHeight = imgHeight * currentScale;

        // Ekran (Grid) boyutları
        double containerWidth = ((Grid)resim.Parent).Width;
        double containerHeight = ((Grid)resim.Parent).Height;

        // Yatay sınır: Resim konteynırdan büyükse hareket alanı = (scaledWidth - containerWidth) / 2
        double maxOffsetX = Math.Max(0, (scaledWidth - containerWidth) / 2);
        double maxOffsetY = Math.Max(0, (scaledHeight - containerHeight) / 2);

        return (-maxOffsetX, maxOffsetX, -maxOffsetY, maxOffsetY);
    }

    private async void OnKapatClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}