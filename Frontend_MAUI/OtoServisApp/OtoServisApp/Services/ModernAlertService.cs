using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using OtoServisApp.Views.Controls;

namespace OtoServisApp.Services;

public static class ModernAlertService
{
    /// <summary>
    /// Aktif sayfaya uyarı görünümünü ekler ve gösterir.
    /// </summary>
    public static async Task<bool?> ShowAsync(string baslik, string mesaj, string butonTipi = "Tamam")
    {
        // UI thread'inde çalıştığımızdan emin ol
        if (!MainThread.IsMainThread)
        {
            return await MainThread.InvokeOnMainThreadAsync(() => ShowAsync(baslik, mesaj, butonTipi));
        }

        var currentPage = GetCurrentPage();
        if (currentPage == null)
            throw new InvalidOperationException("Aktif sayfa bulunamadı.");

        // Sayfanın içeriğini Grid'e çevir (eğer değilse)
        var grid = EnsurePageHasGrid(currentPage);

        // Her seferinde yeni bir ModernAlertView oluştur (eski instance sorunlarını önler)
        var alertView = new ModernAlertView
        {
            ZIndex = 9999
        };

        // Grid'e ekle
        grid.Children.Add(alertView);

        // Göster ve sonucu bekle
        try
        {
            var result = await alertView.ShowAsync(baslik, mesaj, butonTipi);
            return result;
        }
        finally
        {
            // İşlem bitince alertView'i kaldır
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

    // Yardımcı metodlar
    private static Page GetCurrentPage()
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

        // FlyoutPage (varsa)
        if (mainPage is FlyoutPage flyout)
            return flyout.Detail;

        return mainPage;
    }

    private static Grid EnsurePageHasGrid(Page page)
    {
        if (page is ContentPage contentPage)
        {
            if (contentPage.Content is Grid existingGrid)
                return existingGrid;

            // Mevcut içeriği Grid içine al
            var grid = new Grid();
            var oldContent = contentPage.Content;
            contentPage.Content = grid;
            if (oldContent != null)
            {
                grid.Children.Add(oldContent);
            }
            return grid;
        }

        // Eğer ContentPage değilse (nadiren) sayfanın kendisini Grid olarak kabul edemeyiz, hata fırlat
        throw new InvalidOperationException("Sayfa tipi ContentPage değil.");
    }

    // Initialize metodu artık gerekli değil, boş bırakabiliriz veya geriye dönük uyumluluk için tutabiliriz.
    public static void Initialize(Page page)
    {
        // Bu metot artık bir şey yapmıyor, sadece eski kodlar hata vermesin diye var.
    }
}