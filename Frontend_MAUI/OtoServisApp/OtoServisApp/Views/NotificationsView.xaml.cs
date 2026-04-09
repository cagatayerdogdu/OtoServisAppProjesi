using OtoServisApp.Models;
using OtoServisApp.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace OtoServisApp.Views;

public partial class NotificationsView : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isUpdating = false;
    private bool _isSelectAllUpdating = false; // Hepsini seç event'ini geçici engellemek için

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
            return;
        }

        var bildirimler = await _apiService.KullaniciBildirimleriniGetirAsync(aktifKullaniciId.Value);
        Bildirimler.Clear();
        foreach (var item in bildirimler)
        {
            item.IsSelected = false;
            Bildirimler.Add(item);
        }
        // Hepsini seç checkbox'ını sıfırlarken event tetiklenmesini engelle
        _isSelectAllUpdating = true;
        ChkSelectAll.IsChecked = false;
        _isSelectAllUpdating = false;
        BtnDeleteSelected.IsVisible = false;
    }

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
                await BildirimleriYukle();
            }
        }
    }

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
                Bildirimler.Remove(bildirim);
                BtnDeleteSelected.IsVisible = Bildirimler.Any(b => b.IsSelected);
                if (Bildirimler.Count == 0)
                {
                    _isSelectAllUpdating = true;
                    ChkSelectAll.IsChecked = false;
                    _isSelectAllUpdating = false;
                }
                else if (Bildirimler.All(b => b.IsSelected))
                {
                    _isSelectAllUpdating = true;
                    ChkSelectAll.IsChecked = true;
                    _isSelectAllUpdating = false;
                }
            }
        }
    }

    private void OnItemCheckChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isUpdating) return;
        var checkBox = sender as CheckBox;
        var bildirim = checkBox?.BindingContext as BildirimResponse;
        if (bildirim != null)
        {
            bildirim.IsSelected = e.Value;
            BtnDeleteSelected.IsVisible = Bildirimler.Any(b => b.IsSelected);

            // Hepsini seç checkbox'ını güncelle (event tetiklenmesini engelle)
            bool hepsiSecili = Bildirimler.All(b => b.IsSelected);
            if (ChkSelectAll.IsChecked != hepsiSecili)
            {
                _isSelectAllUpdating = true;
                ChkSelectAll.IsChecked = hepsiSecili;
                _isSelectAllUpdating = false;
            }
        }
    }

    private void OnSelectAllCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isSelectAllUpdating) return; // Event'i manuel değişikliklerde tetikleme
        if (_isUpdating) return;
        _isUpdating = true;
        foreach (var item in Bildirimler)
        {
            item.IsSelected = e.Value;
        }
        BtnDeleteSelected.IsVisible = e.Value;
        _isUpdating = false;
    }

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
        BtnDeleteSelected.IsVisible = false;
        _isSelectAllUpdating = true;
        ChkSelectAll.IsChecked = false;
        _isSelectAllUpdating = false;
        await DisplayAlert("Bilgi", "Seçilen bildirimler silindi.", "Tamam");
    }
}