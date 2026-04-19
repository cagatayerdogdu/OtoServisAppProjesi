using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using OtoServisApp.Views.Controls;

namespace OtoServisApp.Services;

public static class ModernAlertService
{
    private static ModernAlertView _alertView;

    public static void Initialize(Page page)
    {
        // Eğer zaten oluşturulmuşsa tekrar ekleme
        if (_alertView != null) return;

        _alertView = new ModernAlertView();

        // Sayfanın içeriğini Grid'e dönüştür ve alertView'i en üst katmana ekle
        if (page is ContentPage contentPage)
        {
            var originalContent = contentPage.Content;
            var grid = new Grid();
            if (originalContent != null)
            {
                // Mevcut içeriği Grid'in ilk çocuğu olarak ekle
                grid.Children.Add(originalContent);
            }
            // AlertView'i Grid'in ikinci çocuğu olarak ekle (üstte görünür)
            grid.Children.Add(_alertView);
            contentPage.Content = grid;
        }
        else if (page is NavigationPage navPage && navPage.CurrentPage is ContentPage currentPage)
        {
            // NavigationPage için mevcut sayfayı hedef al
            Initialize(currentPage);
        }
        else if (page is TabbedPage tabbedPage && tabbedPage.CurrentPage is ContentPage currentTabPage)
        {
            Initialize(currentTabPage);
        }
    }

    public static Task<bool?> ShowAsync(string baslik, string mesaj, string butonTipi = "Tamam")
    {
        if (_alertView == null)
            throw new InvalidOperationException("ModernAlertService başlatılmamış. Lütfen App.xaml.cs içinde Initialize çağırın.");

        return _alertView.ShowAsync(baslik, mesaj, butonTipi);
    }

    public static Task ShowInfoAsync(string mesaj, string baslik = "Bilgi")
        => ShowAsync(baslik, mesaj, "Tamam");

    public static Task<bool> ShowConfirmationAsync(string mesaj, string baslik = "Onay")
        => ShowAsync(baslik, mesaj, "EvetHayir").ContinueWith(t => t.Result == true);

    public static Task<bool?> ShowDeleteConfirmationAsync(string mesaj, string baslik = "Silme Onayı")
        => ShowAsync(baslik, mesaj, "SilVazgec");
}