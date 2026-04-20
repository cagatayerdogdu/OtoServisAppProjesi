using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using OtoServisApp.Views.Controls;

namespace OtoServisApp.Services;

public static class ModernAlertService
{
    public static async Task<bool?> ShowAsync(string baslik, string mesaj, string butonTipi = "Tamam")
    {
        try
        {
            // UI thread'inde olduğumuzdan emin ol
            if (!MainThread.IsMainThread)
            {
                return await MainThread.InvokeOnMainThreadAsync(() => ShowAsync(baslik, mesaj, butonTipi));
            }

            var currentPage = GetCurrentPage();
            if (currentPage == null)
            {
                // Sayfa bulunamazsa konsola yaz ve null dön
                System.Diagnostics.Debug.WriteLine("ModernAlertService: Aktif sayfa bulunamadı.");
                return null;
            }

            // Sayfayı Grid'e çevir
            var grid = EnsurePageHasGrid(currentPage);
            if (grid == null)
            {
                System.Diagnostics.Debug.WriteLine("ModernAlertService: Sayfa Grid'e çevrilemedi.");
                return null;
            }

            // Yeni AlertView oluştur
            var alertView = new ModernAlertView
            {
                ZIndex = 9999
            };

            grid.Children.Add(alertView);

            // Göster ve sonucu bekle
            var result = await alertView.ShowAsync(baslik, mesaj, butonTipi);
            return result;
        }
        catch (Exception ex)
        {
            // Herhangi bir hata olursa uygulama çökmesin, loglansın
            System.Diagnostics.Debug.WriteLine($"ModernAlertService Hatası: {ex.Message}");
            return null;
        }
        finally
        {
            // Temizlik (alertView'i kaldırmak için referansı bulmak zor, bu yüzden her ShowAsync yeni bir tane oluşturuyoruz)
            // İş bitince grid'den kaldırmayı deneyelim
            try
            {
                var currentPage = GetCurrentPage();
                if (currentPage is ContentPage cp && cp.Content is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is ModernAlertView av && av.IsVisible == false)
                        {
                            grid.Children.Remove(av);
                            break;
                        }
                    }
                }
            }
            catch { /* temizlik başarısız olursa sorun değil */ }
        }
    }

    public static Task ShowInfoAsync(string mesaj, string baslik = "Bilgi")
        => ShowAsync(baslik, mesaj, "Tamam");

    public static async Task<bool> ShowConfirmationAsync(string mesaj, string baslik = "Onay")
    {
        var result = await ShowAsync(baslik, mesaj, "EvetHayir");
        return result == true;
    }

    public static async Task<bool?> ShowDeleteConfirmationAsync(string mesaj, string baslik = "Silme Onayı")
    {
        return await ShowAsync(baslik, mesaj, "SilVazgec");
    }

    // ========== YARDIMCI METODLAR ==========
    private static Page GetCurrentPage()
    {
        try
        {
            var mainPage = Application.Current?.MainPage;
            if (mainPage == null) return null;

            // NavigationPage
            if (mainPage is NavigationPage navPage)
                return navPage.CurrentPage ?? mainPage;

            // TabbedPage
            if (mainPage is TabbedPage tabbedPage)
            {
                var currentTab = tabbedPage.CurrentPage;
                if (currentTab is NavigationPage innerNav)
                    return innerNav.CurrentPage ?? currentTab;
                return currentTab;
            }

            // FlyoutPage
            if (mainPage is FlyoutPage flyout)
                return flyout.Detail;

            return mainPage;
        }
        catch
        {
            return null;
        }
    }

    private static Grid EnsurePageHasGrid(Page page)
    {
        try
        {
            if (page is ContentPage contentPage)
            {
                if (contentPage.Content is Grid existingGrid)
                    return existingGrid;

                var grid = new Grid();
                var oldContent = contentPage.Content;
                contentPage.Content = grid;
                if (oldContent != null)
                {
                    grid.Children.Add(oldContent);
                }
                return grid;
            }
        }
        catch { }
        return null;
    }

    // Eski kodlar hata vermesin diye boş Initialize metodu
    public static void Initialize(Page page) { }
}