using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using OtoServisApp.Views.Controls;

namespace OtoServisApp.Services;

public static class ModernAlertService
{
    private static ModernAlertView _alertView;
    private static Page _currentPage;

    public static void Initialize(Page page)
    {
        _currentPage = page;
        if (_alertView != null) return;

        _alertView = new ModernAlertView();
        AttachToPage(page);
    }

    private static void AttachToPage(Page page)
    {
        if (page == null) return;

        if (page is ContentPage cp)
        {
            var grid = cp.Content as Grid;
            if (grid == null)
            {
                var oldContent = cp.Content;
                grid = new Grid();
                if (oldContent != null)
                    grid.Children.Add(oldContent);
                cp.Content = grid;
            }
            if (!grid.Children.Contains(_alertView))
                grid.Children.Add(_alertView);
        }
        else if (page is NavigationPage nav && nav.CurrentPage != null)
        {
            AttachToPage(nav.CurrentPage);
        }
        else if (page is TabbedPage tab && tab.CurrentPage != null)
        {
            AttachToPage(tab.CurrentPage);
        }
    }

    public static Task<bool?> ShowAsync(string baslik, string mesaj, string butonTipi = "Tamam")
    {
        if (_alertView == null)
            throw new InvalidOperationException("ModernAlertService başlatılmamış. Lütfen App.xaml.cs içinde Initialize çağırın.");

        // Sayfa değişmiş olabilir, kontrol et ve yeniden bağla
        var currentPage = Application.Current?.MainPage;
        if (currentPage != null && currentPage != _currentPage)
        {
            _currentPage = currentPage;
            AttachToPage(currentPage);
        }

        return _alertView.ShowAsync(baslik, mesaj, butonTipi);
    }

    public static Task ShowInfoAsync(string mesaj, string baslik = "Bilgi")
        => ShowAsync(baslik, mesaj, "Tamam");

    public static Task<bool> ShowConfirmationAsync(string mesaj, string baslik = "Onay")
        => ShowAsync(baslik, mesaj, "EvetHayir").ContinueWith(t => t.Result == true);

    public static Task<bool?> ShowDeleteConfirmationAsync(string mesaj, string baslik = "Silme Onayı")
        => ShowAsync(baslik, mesaj, "SilVazgec");
}