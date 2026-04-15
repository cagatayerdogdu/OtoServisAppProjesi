using OtoServisApp.Models;

namespace OtoServisApp.Views;

public partial class FullScreenPhotoView : ContentPage
{
    public FullScreenPhotoView(List<ServisTalebiFotograf> fotolar, int index)
    {
        InitializeComponent();

        FotoCarousel.ItemsSource = fotolar;
        FotoCarousel.Position = index;
    }

    private void OnZoomStateChanged(bool isZoomed)
    {
        FotoCarousel.IsSwipeEnabled = !isZoomed;
    }

    private async void OnKapatClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}