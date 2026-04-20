using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using OtoServisApp.Views.Controls;

namespace OtoServisApp.Services;

public static class ModernAlertService
{
    public static async Task<bool?> ShowAsync(string baslik, string mesaj, string butonTipi = "Tamam")
    {
        if (!MainThread.IsMainThread)
            return await MainThread.InvokeOnMainThreadAsync(() => ShowAsync(baslik, mesaj, butonTipi));

        var currentPage = GetCurrentPage();
        if (currentPage == null)
            throw new InvalidOperationException("ModernAlertService: Aktif sayfa bulunamadı.");

        // Sayfanın root'unu Grid yap (değilse)
        var grid = EnsurePageHasGrid(currentPage);

        var alertView = new ModernAlertView
        {
            ZIndex = 9999,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };

        grid.Children.Add(alertView);

        try
        {
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
            if (grid.Children.Contains(alertView))
                grid.Children.Remove(alertView);
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
        var mainPage = Application.Current?.MainPage;
        if (mainPage == null) return null;

        if (mainPage is NavigationPage navPage)
            return navPage.CurrentPage ?? mainPage;

        if (mainPage is TabbedPage tabbedPage)
        {
            var currentTab = tabbedPage.CurrentPage;
            if (currentTab is NavigationPage innerNav)
                return innerNav.CurrentPage ?? currentTab;
            return currentTab;
        }

        if (mainPage is FlyoutPage flyout)
            return flyout.Detail;

        return mainPage;
    }

    private static Grid EnsurePageHasGrid(Page page)
    {
        if (page is ContentPage contentPage)
        {
            // Eğer zaten Grid ise direkt kullan
            if (contentPage.Content is Grid existingGrid)
                return existingGrid;

            // Değilse, mevcut içeriği yeni bir Grid'e sar
            var grid = new Grid
            {
                // Grid'in tüm sayfayı kaplamasını sağla
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            var oldContent = contentPage.Content;
            contentPage.Content = grid;

            if (oldContent != null)
            {
                // Eski içeriği Grid'e ekle ve tüm alanı kaplamasını sağla
                if (oldContent is View oldView)
                {
                    oldView.HorizontalOptions = LayoutOptions.Fill;
                    oldView.VerticalOptions = LayoutOptions.Fill;
                }
                grid.Children.Add(oldContent);
            }

            return grid;
        }

        throw new InvalidOperationException("Sayfa tipi ContentPage değil.");
    }

    // Eski kodlar hata vermesin diye boş Initialize metodu													
    public static void Initialize(Page page) { }
}