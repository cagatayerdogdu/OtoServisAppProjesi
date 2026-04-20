using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace OtoServisApp.Services;

public static class SecureStorageHelper
{
    private const string KeyUserId = "kullanici_id_gizli";
    private const string KeySavedEmail = "kayitli_eposta";
    private const string KeySavedPassword = "kayitli_sifre";

    /// <summary>
    /// Güvenli bir şekilde değer okur. Hata olursa null döner ve bozuk anahtarları temizler.
    /// </summary>
    public static async Task<string> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SecureStorage GetAsync hatası ({key}): {ex.Message}");
            await CleanupCorruptedKeysAsync();
            return null;
        }
    }

    /// <summary>
    /// Güvenli bir şekilde değer yazar. Hata olursa sessizce başarısız olur.
    /// </summary>
    public static async Task<bool> SetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SecureStorage SetAsync hatası ({key}): {ex.Message}");
            await CleanupCorruptedKeysAsync();
            return false;
        }
    }

    /// <summary>
    /// Güvenli bir şekilde anahtar siler.
    /// </summary>
    public static void Remove(string key)
    {
        try
        {
            SecureStorage.Default.Remove(key);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SecureStorage Remove hatası ({key}): {ex.Message}");
            // Temizlik denemesi
            _ = CleanupCorruptedKeysAsync();
        }
    }

    /// <summary>
    /// Bozuk tüm SecureStorage anahtarlarını temizler.
    /// </summary>
    public static async Task CleanupCorruptedKeysAsync()
    {
        try
        {
            // Tüm bilinen anahtarları tek tek silmeyi dene
            Remove(KeyUserId);
            Remove(KeySavedEmail);
            Remove(KeySavedPassword);

            // Ayrıca var olabilecek diğer anahtarları da temizlemek için
            // SecureStorage'ın tümünü temizlemenin bir yolu yok, bu yüzden bilinenleri siliyoruz.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CleanupCorruptedKeysAsync hatası: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    // --- Kolay Erişim Metotları ---
    public static Task<string> GetUserIdAsync() => GetAsync(KeyUserId);
    public static Task<bool> SetUserIdAsync(string value) => SetAsync(KeyUserId, value);
    public static void RemoveUserId() => Remove(KeyUserId);

    public static Task<string> GetSavedEmailAsync() => GetAsync(KeySavedEmail);
    public static Task<bool> SetSavedEmailAsync(string value) => SetAsync(KeySavedEmail, value);
    public static void RemoveSavedEmail() => Remove(KeySavedEmail);

    public static Task<string> GetSavedPasswordAsync() => GetAsync(KeySavedPassword);
    public static Task<bool> SetSavedPasswordAsync(string value) => SetAsync(KeySavedPassword, value);
    public static void RemoveSavedPassword() => Remove(KeySavedPassword);
}