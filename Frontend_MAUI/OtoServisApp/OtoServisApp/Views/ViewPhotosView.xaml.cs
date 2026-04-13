using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class ViewPhotosView : ContentPage
{
    private readonly ApiService _apiService;
    private ServisTalebi _secilenTalep;

    public ViewPhotosView(ServisTalebi talep)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _secilenTalep = talep;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await FotograflariYukle();
    }

    private async Task FotograflariYukle()
    {
        var fotolar = await _apiService.TalepFotograflariniGetirAsync(_secilenTalep.id);

        if (fotolar != null && fotolar.Any())
        {
            BindableLayout.SetItemsSource(PhotosLayout, fotolar);
        }
        else
        {
            await DisplayAlert("Bilgi", "Bu talebe ait fotoğraf bulunamadı.", "Tamam");
            await Navigation.PopAsync();
        }
    }

    // Fotoğrafın üzerine tıklandığında geçici bir tam ekran görüntüleyici açar
    // YENİ REVİZE: Parametreyi TappedEventArgs üzerinden yakalıyoruz. Görselleri büyütebilmek için.
    private async void OnPhotoTapped(object sender, TappedEventArgs e)
    {
        // 1. Tüm fotoğraf listesini alıyoruz
        var fotolar = (List<ServisTalebiFotograf>)BindableLayout.GetItemsSource(PhotosLayout);

        // 2. Tıklanan fotoğrafın URL'sini alıyoruz
        string tiklananUrl = e.Parameter as string;

        if (fotolar != null && !string.IsNullOrEmpty(tiklananUrl))
        {
            // 3. Tıklanan resmin listedeki kaçıncı sırada olduğunu buluyoruz
            int indeks = fotolar.FindIndex(f => f.TamUrl == tiklananUrl);

            // 4. Yeni gelişmiş tam ekran sayfamızı açıyoruz
            await Navigation.PushModalAsync(new FullScreenPhotoView(fotolar, indeks));
        }
    }

    // YENİ REVİZE: Fotoğraf Silme İşlemi
    private async void OnDeletePhotoClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var foto = btn?.CommandParameter as ServisTalebiFotograf;

        if (foto != null)
        {
            bool onay = await DisplayAlert("Onay", "Bu fotoğrafı kalıcı olarak silmek istediğinize emin misiniz?", "Evet, Sil", "Vazgeç");
            if (onay)
            {
                bool silindi = await _apiService.FotografSilAsync(foto.id);
                if (silindi)
                {
                    await DisplayAlert("Başarılı", "Fotoğraf başarıyla silindi.", "Tamam");
                    await FotograflariYukle(); // Listeyi ekrandan tazele
                }
                else
                {
                    await DisplayAlert("Hata", "Fotoğraf silinirken bir sorun oluştu.", "Tamam");
                }
            }
        }
    }
}