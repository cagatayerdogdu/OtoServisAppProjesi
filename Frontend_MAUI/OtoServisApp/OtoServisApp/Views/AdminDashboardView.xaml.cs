using OtoServisApp.Models; // Bunu en üste eklemeyi unutma

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
        await Navigation.PushAsync(new AdminRequestsView());
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
        await Navigation.PushAsync(new ShowcaseView());
    }
}