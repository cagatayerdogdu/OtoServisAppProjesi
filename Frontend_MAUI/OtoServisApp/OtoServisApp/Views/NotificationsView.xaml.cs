using OtoServisApp.Models;
using OtoServisApp.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace OtoServisApp.Views;

public partial class NotificationsView : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isUpdating = false;
    private int _seciliSayisi = 0;

    // Bildirim listesi için ObservableCollection kullanıyoruz (seçim durumu için)																	
    public ObservableCollection<BildirimResponse> Bildirimler { get; set; } = new();

    public NotificationsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
        NotificationList.ItemsSource = Bildirimler;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await BildirimleriYukle();
    }

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
            Bildirimler.Clear();
            _seciliSayisi = 0;
            return;
        }

        var bildirimler = await _apiService.KullaniciBildirimleriniGetirAsync(aktifKullaniciId.Value);
        Bildirimler.Clear();
        foreach (var item in bildirimler)
        {
            item.IsSelected = false; // Yeni yüklenenlerde seçim yok
            Bildirimler.Add(item);
        }
        _seciliSayisi = 0;
        ChkSelectAll.IsChecked = false;
        BtnDeleteSelected.IsVisible = false;
    }

    // Kısa tıklama: okundu işaretle
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
                // UI otomatik güncellenir (DataTrigger ile)
            }
        }
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
                if (bildirim.IsSelected) _seciliSayisi--;
                Bildirimler.Remove(bildirim);
                BtnDeleteSelected.IsVisible = _seciliSayisi > 0;
                if (Bildirimler.Count == 0) ChkSelectAll.IsChecked = false;
                else if (_seciliSayisi == Bildirimler.Count) ChkSelectAll.IsChecked = true;
            }
        }
    }

    // CheckBox değiştiğinde (tek tek seçim)
    private void OnItemCheckChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isUpdating) return;
        var checkBox = sender as CheckBox;
        var bildirim = checkBox?.BindingContext as BildirimResponse;
        if (bildirim != null)
        {
            bildirim.IsSelected = e.Value;
            _seciliSayisi += e.Value ? 1 : -1;
            BtnDeleteSelected.IsVisible = _seciliSayisi > 0;

            // Hepsini seç checkbox'ını güncelle
            if (_seciliSayisi == Bildirimler.Count)
                ChkSelectAll.IsChecked = true;
            else
                ChkSelectAll.IsChecked = false;
        }
    }

    // Hepsini seç checkbox'ı değiştiğinde
    private void OnSelectAllCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        _isUpdating = true;
        foreach (var item in Bildirimler)
        {
            item.IsSelected = e.Value;
        }
        _seciliSayisi = e.Value ? Bildirimler.Count : 0;
        BtnDeleteSelected.IsVisible = e.Value;
        _isUpdating = false;
    }

    // Seçilenleri sil butonu
    private async void OnDeleteSelectedClicked(object sender, EventArgs e)
    {
        var secilenler = Bildirimler.Where(b => b.IsSelected).ToList();
        if (!secilenler.Any()) return;

        bool onay = await DisplayAlert("Onay", $"{secilenler.Count} adet bildirimi silmek istiyor musunuz?", "Evet", "İptal");
        if (!onay) return;

        foreach (var bildirim in secilenler)
        {
            await _apiService.NotificationsDeleteAsync($"bildirimler/{bildirim.id}");
            Bildirimler.Remove(bildirim);
        }
        _seciliSayisi = 0;
        BtnDeleteSelected.IsVisible = false;
        ChkSelectAll.IsChecked = false;
        await DisplayAlert("Bilgi", "Seçilen bildirimler silindi.", "Tamam");
    }
}