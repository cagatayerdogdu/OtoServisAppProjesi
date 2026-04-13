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
            resim.AnchorX = 0.5;
            resim.AnchorY = 0.5;
        }
        else if (e.Status == GestureStatus.Running)
        {
            currentScale += (e.Scale - 1) * startScale;
            // Ölçeği 1 ile 5 arasında zorla sınırla (daha da küçülememesini sağlar)
            currentScale = Math.Max(1, Math.Min(currentScale, 5));

            resim.Scale = currentScale;

            // KİLİT ÇÖZÜM: Kullanıcı resmi 1x boyutuna (veya çok yakınına) kadar küçülttüyse, 
            // kayma eksenlerini (X ve Y) anında sıfırla ki resim köşelere kaçmasın!
            if (currentScale <= 1.05)
            {
                resim.TranslationX = 0;
                resim.TranslationY = 0;
                xOffset = 0;
                yOffset = 0;
            }

            FotoCarousel.IsSwipeEnabled = currentScale <= 1.05;
        }
        else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            // Kullanıcı parmaklarını çektiğinde resim biraz küçülmüşse tam merkeze ve orijinal boyuta oturt
            if (currentScale <= 1.05)
            {
                currentScale = 1;
                resim.ScaleTo(1, 200, Easing.CubicOut);
                resim.TranslateTo(0, 0, 200, Easing.CubicOut);
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

        // KİLİT ÇÖZÜM 2: Resim orijinal boyutundayken kaydırma (Pan) komutlarını tamamen yok say!
        if (resim == null || currentScale <= 1.05) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
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