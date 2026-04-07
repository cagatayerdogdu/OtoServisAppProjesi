using OtoServisApp.Models;
using OtoServisApp.Helpers;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

namespace OtoServisApp.Views;

public partial class MainTabbedPage : Microsoft.Maui.Controls.TabbedPage
{
    public MainTabbedPage(Kullanici kullanici)
    {
        InitializeComponent();

        // NavigationPage özelliklerinde varsayılan siyah barı engellemek için Turkuaz renk veriyoruz
        // .png yerine projede tanımlı FontAwesome ikonlarımızı (FASolid) FontImageSource ile yapılandırıyoruz
        /*var dashNav = new NavigationPage(new DashboardView(kullanici))
        {
            Title = "Anasayfa",
            IconImageSource = new FontImageSource { Glyph = IconFont.Home, FontFamily = "FASolid", Size = 24 },
            BarBackgroundColor = Color.FromArgb("#00BCD4"),
            BarTextColor = Colors.White
        };

        var requestsNav = new NavigationPage(new MyServiceRequestsView(kullanici))
        {
            Title = "Taleplerim",
            IconImageSource = new FontImageSource { Glyph = IconFont.Wrench, FontFamily = "FASolid", Size = 24 },
            BarBackgroundColor = Color.FromArgb("#00BCD4"),
            BarTextColor = Colors.White
        };

        var vehiclesNav = new NavigationPage(new VehiclesView(kullanici))
        {
            Title = "Araçlarım",
            IconImageSource = new FontImageSource { Glyph = IconFont.Car, FontFamily = "FASolid", Size = 24 },
            BarBackgroundColor = Color.FromArgb("#00BCD4"),
            BarTextColor = Colors.White
        };

        var profileNav = new NavigationPage(new ProfileView(kullanici))
        {
            Title = "Profil",
            IconImageSource = new FontImageSource { Glyph = IconFont.User, FontFamily = "FASolid", Size = 24 },
            BarBackgroundColor = Color.FromArgb("#00BCD4"),
            BarTextColor = Colors.White
        };

        // Sekmeleri alt bara ekliyoruz
        Children.Add(dashNav);
        Children.Add(requestsNav);
        Children.Add(vehiclesNav);
        Children.Add(profileNav);*/

        // --- ANDROID ÖZEL AYARLARI ---
        // Navbar'ı en alta sabitler. İsim çakışmasını önlemek için tam yol belirtilmiştir.
        On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);

        // Sağa sola kaydırarak sayfa değiştirmeyi kapatır (İleride istersen true yapabilirsin)
        On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().SetIsSwipePagingEnabled(false);

        // Sayfaları ve İkonları Hazırla
        Children.Add(CreateTab(new DashboardView(kullanici), "Anasayfa", IconFont.Home));
        Children.Add(CreateTab(new MyServiceRequestsView(kullanici), "Taleplerim", IconFont.Wrench));
        Children.Add(CreateTab(new VehiclesView(kullanici), "Araçlarım", IconFont.Car));
        Children.Add(CreateTab(new ProfileView(kullanici), "Profil", IconFont.User));
    }

    private NavigationPage CreateTab(ContentPage page, string title, string icon)
    {
        var navPage = new NavigationPage(page)
        {
            Title = title,
            IconImageSource = new FontImageSource
            {
                Glyph = icon,
                FontFamily = "FASolid",
                Size = 24
            },
            BarBackgroundColor = Color.FromArgb("#00BCD4"),
            BarTextColor = Colors.White
        };

        return navPage;
    }

    private bool _isBackPressedOnce = false;

    protected override bool OnBackButtonPressed()
    {
        var currentNavPage = CurrentPage as NavigationPage;

        // 1. DURUM: Eğer sekmenin içinde alt bir sayfadaysak (Örn: Profil'den Şifre Değiştir'e girmişse)
        // Normal bir şekilde geri gelmesine (PopAsync) izin ver.
        if (currentNavPage != null && currentNavPage.Navigation.NavigationStack.Count > 1)
        {
            return base.OnBackButtonPressed();
        }

        // 2. DURUM: Kök sayfadayız (Ana sekmelerden birindeyiz) ve ikinci kez kaydırma yapıldı.
        if (_isBackPressedOnce)
        {
            // ÇAKIŞMA ÇÖZÜMÜ: Hangi Application olduğunu tam adıyla belirtiyoruz
            Microsoft.Maui.Controls.Application.Current.Quit();
            return true;
        }

        // 3. DURUM: İlk kez kaydırma yapıldı. Uyarı ver.
        _isBackPressedOnce = true;

        // ÇAKIŞMA ÇÖZÜMÜ: Doğrudan sayfanın kendi DisplayAlert metodunu Dispatcher ile güvenli şekilde çağırıyoruz
        Dispatcher.Dispatch(async () =>
        {
            await DisplayAlert("Çıkış", "Uygulamadan çıkmak için tekrar geri kaydırın.", "Tamam");
        });

        // 2 saniye içinde tekrar kaydırmazsa durumu sıfırla
        Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
        {
            _isBackPressedOnce = false;
            return false; // Timer'ı durdur
        });

        // Sistemin kendi kendine çıkış yapmasını engellemek için true döndürüyoruz
        return true;
    }
}
