using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Android.Security.Keystore;
using Java.Security;
using Java.IO;
using Android.App;
using Android.Content;

namespace OtoServisApp.Services;

public static class SecureStorageHelper
{
    private const string KeyUserId = "kullanici_id_gizli";
    private const string KeySavedEmail = "kayitli_eposta";
    private const string KeySavedPassword = "kayitli_sifre";

    private static readonly string[] AllKeys = { KeyUserId, KeySavedEmail, KeySavedPassword };

    /// <summary>
    /// SecureStorage'dan güvenli bir şekilde değer okur. Hata durumunda anahtar temizliği yapar ve tekrar dener.
    /// </summary>
    public static async Task<string> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch (Exception ex) when (ex is Java.Security.AEADBadTagException || ex.Message.Contains("AEADBadTagException"))
        {
            Debug.WriteLine($"[SecureStorage] Bozuk anahtar tespit edildi: {key}. Temizlik başlatılıyor...");
            await CleanupKeyFromKeyStoreAsync(key);
            // Tekrar dene
            return await SecureStorage.Default.GetAsync(key);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SecureStorage] GetAsync hatası ({key}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// SecureStorage'a güvenli bir şekilde değer yazar. Hata durumunda anahtar temizliği yapar ve tekrar dener.
    /// </summary>
    public static async Task<bool> SetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
            return true;
        }
        catch (Exception ex) when (ex is Java.Security.AEADBadTagException || ex.Message.Contains("AEADBadTagException"))
        {
            Debug.WriteLine($"[SecureStorage] SetAsync sırasında bozuk anahtar: {key}. Temizleniyor...");
            await CleanupKeyFromKeyStoreAsync(key);
            await SecureStorage.Default.SetAsync(key, value);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SecureStorage] SetAsync hatası ({key}): {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// SecureStorage'dan anahtar siler. Hata olsa bile temizlik yapmaya çalışır.
    /// </summary>
    public static void Remove(string key)
    {
        try
        {
            SecureStorage.Default.Remove(key);
        }
        catch (Exception ex) when (ex is Java.Security.AEADBadTagException || ex.Message.Contains("AEADBadTagException"))
        {
            Debug.WriteLine($"[SecureStorage] Remove sırasında bozuk anahtar: {key}. Zorla temizleniyor...");
            _ = CleanupKeyFromKeyStoreAsync(key);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SecureStorage] Remove hatası ({key}): {ex.Message}");
        }
    }

    /// <summary>
    /// Uygulamanın kullandığı tüm SecureStorage anahtarlarını temizler.
    /// </summary>
    public static async Task CleanupAllKeysAsync()
    {
        foreach (var key in AllKeys)
        {
            await CleanupKeyFromKeyStoreAsync(key);
        }
    }

    /// <summary>
    /// Android KeyStore'da belirli bir anahtarın alias'ını siler.
    /// </summary>
    private static Task CleanupKeyFromKeyStoreAsync(string key)
    {
        return Task.Run(() =>
        {
#if ANDROID
            try
            {
                // MAUI SecureStorage'ın kullandığı alias formatı:
                // "{App.PackageName}.xamarinessentials.keys" veya "{App.PackageName}.microsoft.maui.keys"
                // Gerçek alias'ı bulmak için KeyStore'daki tüm girişleri kontrol edebiliriz.
                var keyStore = KeyStore.GetInstance("AndroidKeyStore");
                keyStore.Load(null);

                var aliases = keyStore.Aliases();
                while (aliases.HasMoreElements)
                {
                    var alias = aliases.NextElement().ToString();
                    if (alias.Contains(key, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            keyStore.DeleteEntry(alias);
                            Debug.WriteLine($"[SecureStorage] KeyStore'dan silindi: {alias}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[SecureStorage] Alias silinemedi: {alias} - {ex.Message}");
                        }
                    }
                }

                // Ayrıca bilinen olası alias'ları da dene
                var possibleAliases = new[]
                {
                    $"{Application.Context.PackageName}.xamarinessentials.keys",
                    $"{Application.Context.PackageName}.microsoft.maui.keys",
                    $"{Application.Context.PackageName}_xamarinessentials",
                    $"{Application.Context.PackageName}_microsoft.maui"
                };

                foreach (var alias in possibleAliases)
                {
                    try
                    {
                        keyStore.DeleteEntry(alias);
                        Debug.WriteLine($"[SecureStorage] Bilinen alias silindi: {alias}");
                    }
                    catch { /* Sessiz geç */ }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SecureStorage] KeyStore temizleme hatası: {ex.Message}");
            }
#endif
        });
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