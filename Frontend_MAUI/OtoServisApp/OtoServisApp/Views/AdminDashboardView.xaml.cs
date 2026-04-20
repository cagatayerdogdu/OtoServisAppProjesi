using OtoServisApp.Models;

namespace OtoServisApp.Views;

public partial class AdminDashboardView : ContentPage
{
    private Kullanici _aktifKullanici; // Patronu hafızada tutacağımız değişken

    // İŞTE BURAYA KULLANICI PARAMETRESİNİ EKLEDİK
    public AdminDashboardView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
    }

    private async void OnManageRequestsTapped(object sender, EventArgs e)
    {
        // Talepler ekranına geçiş yap
        await Navigation.PushAsync(new AdminServiceView());    // AdminRequestsView
    }

    private async void OnPastRequestsTapped(object sender, EventArgs e)
    {
        // Geçmiş talepler sayfasına yönlendirir
        await Navigation.PushAsync(new AdminPastRequestsView());
    }

    private async void OnSistemLoglariClicked(object sender, EventArgs e)
    {
        // Kara Kutuya (Log sayfasına) geçiş yapıyoruz!
        await Navigation.PushAsync(new AdminLogsView());
    }

    private async void OnShowcaseManageTapped(object sender, EventArgs e)
    {
        // Vitrin (İşlerimiz) sayfasına yönlendirme
        await Navigation.PushAsync(new AdminShowcaseManageView());
    }

    private async void OnPriceManagementTapped(object sender, EventArgs e)
    {
        // Fiyat Yönetimi sayfasına geçiş yap
        await Navigation.PushAsync(new AdminPriceManagementView());
    }

    private async void OnUserManagementTapped(object sender, EventArgs e)
    {
        // Kullanıcı yönetimi sayfasına yönlendir
        await Navigation.PushAsync(new AdminUserManagementView());
    }

    private async void OnUserTrackingTapped(object sender, EventArgs e)
    {
        // Yeni oluşturduğumuz takip ekranına uçuyoruz
        await Navigation.PushAsync(new AdminUserTrackingView());
    }

}