using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class NotificationsView : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isBatchSelecting = false; // Toplu seçim sırasında event tetiklenmesini engeller

    public NotificationsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await BildirimleriYukle();
    }

    // YENİ REVİZE: Kullanıcı ID'yi SecureStorage'dan al
    private async Task<int?> GetCurrentUserIdAsync()
    {
        string idStr = await SecureStorage.Default.GetAsync("kullanici_id_gizli");
        if (int.TryParse(idStr, out int id))
            return id;
        return null;
    }

    private async Task BildirimleriYukle()
    {
        int? aktifKullaniciId = await GetCurrentUserIdAsync();
        if (aktifKullaniciId == null)
        {
            // Kullanıcı ID yoksa hata göster veya boş liste gönder
            NotificationList.ItemsSource = new List<BildirimResponse>();
            return;
        }

        var bildirimler = await _apiService.KullaniciBildirimleriniGetirAsync(aktifKullaniciId.Value);
        // Seçimleri temizle ve Hepsini Seç checkbox'ını sıfırla
        NotificationList.ItemsSource = null;
        NotificationList.ItemsSource = bildirimler;
    }

    // REVİZE: Tek tıklamada okundu işaretle, seçim yapma
    private async void OnNotificationTapped(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var bildirim = border?.BindingContext as BildirimResponse;

        if (bildirim != null && !bildirim.okundu_mu)
        {
            bool basarili = await _apiService.BildirimOkunduIsaretleAsync(bildirim.id);
            if (basarili)
            {
                bildirim.okundu_mu = true;
                // Sadece ilgili öğenin görünümünü güncellemek için tüm listeyi yeniden yükle
                await BildirimleriYukle();
            }
        }
    }

    // Hepsini Seç checkbox'ı değiştiğinde
    private void OnSelectAllCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        _isBatchSelecting = true;
        var allItems = NotificationList.ItemsSource as IEnumerable<BildirimResponse>;
        if (allItems != null)
        {
            if (e.Value)
            {
                // Hepsini seç: Henüz seçili değilse ekle										 
                foreach (var item in allItems)
                {
                    if (!NotificationList.SelectedItems.Contains(item))
                        NotificationList.SelectedItems.Add(item);
                }
            }
            else
            {
                NotificationList.SelectedItems.Clear();
            }
        }
        _isBatchSelecting = false; // Kilidi aç
        BtnDeleteSelected.IsVisible = NotificationList.SelectedItems.Count > 0;
    }

    // Seçim değiştiğinde (manuel seçimleri engellemek için bu event'i kullanmıyoruz)
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBatchSelecting) return;
        // Sadece silme butonunun görünürlüğünü güncelle
        BtnDeleteSelected.IsVisible = NotificationList.SelectedItems.Count > 0;

        // Eğer tüm elemanlar seçili değilse checkbox'ı kaldır
        var allItems = NotificationList.ItemsSource as IEnumerable<BildirimResponse>;
        if (allItems != null && ChkSelectAll.IsChecked)
        {
            int seciliSayisi = NotificationList.SelectedItems.Count;
            int toplamSayi = allItems.Count();
            if (seciliSayisi != toplamSayi)
                ChkSelectAll.IsChecked = false;
        }
    }

    // Seçilenleri sil butonu
    private async void OnDeleteSelectedClicked(object sender, EventArgs e)
    {
        var secilenler = NotificationList.SelectedItems.Cast<BildirimResponse>().ToList();
        if (!secilenler.Any()) return;

        bool onay = await DisplayAlert("Onay", $"{secilenler.Count} adet bildirimi silmek istiyor musunuz?", "Evet", "İptal");
        if (!onay) return;

        foreach (var bildirim in secilenler)
        {
            await _apiService.NotificationsDeleteAsync($"bildirimler/{bildirim.id}");
        }

        // Seçimleri temizle
        NotificationList.SelectedItems.Clear();
        ChkSelectAll.IsChecked = false;
        await BildirimleriYukle();
        await DisplayAlert("Bilgi", "Seçilen bildirimler silindi.", "Tamam");
    }

    // Tek bildirim silme (sola kaydırma)
    private async void OnSingleDeleteInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItemView;
        var bildirim = swipeItem?.BindingContext as BildirimResponse;

        if (bildirim != null)
        {
            bool onay = await DisplayAlert("Onay", "Bu bildirimi silmek istiyor musunuz?", "Evet", "Vazgeç");
            if (onay)
            {
                await _apiService.NotificationsDeleteAsync($"bildirimler/{bildirim.id}");
                await BildirimleriYukle();
            }
        }
    }
}