using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using OtoServisApp.Views.Controls;

namespace OtoServisApp.Services;

public static class ModernAlertService
{
    private static ModernAlertView _alertView;
    private static Page _currentPage;

    /// <summary>
    /// Verilen sayfaya ModernAlertView'i ekler. Eğer daha önce başka bir sayfaya eklenmişse, oradan kaldırıp yenisine taşır.
    /// </summary>
    public static void Initialize(Page page)
    {
        if (page == null) return;

        // Eğer alertView daha önce oluşturulmamışsa oluştur
        if (_alertView == null)
            _alertView = new ModernAlertView();

        // Eğer aynı sayfaya zaten ekliyse tekrar ekleme
        if (_currentPage == page) return;

        // Önceki sayfadan kaldır
        if (_currentPage is ContentPage oldContentPage)
        {
            var oldGrid = oldContentPage.Content as Grid;
            if (oldGrid != null && oldGrid.Children.Contains(_alertView))
                oldGrid.Children.Remove(_alertView);
        }

        // Yeni sayfaya ekle
        if (page is ContentPage newContentPage)
        {
            var originalContent = newContentPage.Content;
            var grid = new Grid();

            if (originalContent != null)
                grid.Children.Add(originalContent);

            grid.Children.Add(_alertView);
            newContentPage.Content = grid;
        }
        else if (page is NavigationPage navPage && navPage.CurrentPage is ContentPage navCurrentPage)
        {
            Initialize(navCurrentPage);
        }
        else if (page is TabbedPage tabbedPage && tabbedPage.CurrentPage is ContentPage tabCurrentPage)
        {
            Initialize(tabCurrentPage);
        }

        _currentPage = page;
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