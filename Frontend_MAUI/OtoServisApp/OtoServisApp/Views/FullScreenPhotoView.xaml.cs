namespace OtoServisApp.Views;

public partial class FullScreenPhotoView : ContentPage
{
    public FullScreenPhotoView(string imageUrl)
    {
        InitializeComponent();

        // Sayfa açılırken URL'yi alıp resme basıyoruz
        TamEkranResim.Source = imageUrl;
    }

    private async void OnKapatClicked(object sender, EventArgs e)
    {
        // Kapat butonuna basıldığında modal ekranı kapatıp eski sayfaya dönüyoruz
        await Navigation.PopModalAsync();
    }
}