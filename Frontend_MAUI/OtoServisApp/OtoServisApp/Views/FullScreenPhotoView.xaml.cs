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
            startScale = resim.Scale;
            // KİLİT ÇÖZÜM 1: Zıplamalara sebep olan Anchor (Çapa) değiştirme kodlarını tamamen SİLDİK.
        }
        else if (e.Status == GestureStatus.Running)
        {
            currentScale += (e.Scale - 1) * startScale;
            currentScale = Math.Max(1, Math.Min(currentScale, 4)); // Max 4x büyütebilsin
            resim.Scale = currentScale;

            // KİLİT ÇÖZÜM 2: Uygulamayı çökerten IsSwipeEnabled kodunu buradan SİLDİK.
        }
        else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            if (currentScale <= 1.05)
            {
                // Resim küçültüldüyse güvenli bir şekilde eski merkezine oturt
                currentScale = 1;
                resim.ScaleTo(1, 200, Easing.CubicInOut);
                resim.TranslateTo(0, 0, 200, Easing.CubicInOut);
                xOffset = 0;
                yOffset = 0;

                // İşlem bittiği için artık fotoğraflar arası kaydırmaya izin ver
                FotoCarousel.IsSwipeEnabled = true;
            }
            else
            {
                // Resim hala büyükse (zoomluysa), parmakla gezinirken diğer fotoğrafa geçmemesi için Carousel'i kilitle
                FotoCarousel.IsSwipeEnabled = false;
            }
        }
    }

    // --- PAN (YAKINLAŞINCA RESİM İÇİNDE GEZİNME) MANTIĞI ---
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var resim = sender as Image;

        // Resim orijinal boyuttaysa gezinme kodlarını iptal et
        if (resim == null || currentScale <= 1.05) return;

        if (e.StatusType == GestureStatus.Running)
        {
            // Sadece resim büyümüşse içinde sağa sola gezinmeye izin ver
            resim.TranslationX = xOffset + e.TotalX;
            resim.TranslationY = yOffset + e.TotalY;
        }
        else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
        {
            // Gezinme bittiğinde son koordinatları hafızaya al
            xOffset = resim.TranslationX;
            yOffset = resim.TranslationY;
        }
    }

    private async void OnKapatClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}