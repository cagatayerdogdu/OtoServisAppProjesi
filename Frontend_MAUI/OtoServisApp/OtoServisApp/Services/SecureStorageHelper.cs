using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

#if ANDROID
using Android.App;
using Java.Security;
#endif

namespace OtoServisApp.Services;

public static class SecureStorageHelper
{
    private const string KeyUserId = "kullanici_id_gizli";
    private const string KeySavedEmail = "kayitli_eposta";
    private const string KeySavedPassword = "kayitli_sifre";

    private static bool _usePreferencesFallback = false;

    public static async Task<string> GetAsync(string key)
    {
        if (_usePreferencesFallback)
            return Preferences.Default.Get<string>(key, null);

        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SecureStorage] GetAsync kalıcı hata: {ex.Message}");
            _usePreferencesFallback = true;
            await CleanupAllKeysAsync();
            return Preferences.Default.Get<string>(key, null);
        }
    }

    public static async Task<bool> SetAsync(string key, string value)
    {
        if (_usePreferencesFallback)
        {
            Preferences.Default.Set(key, value);
            return true;
        }

        try
        {
            await SecureStorage.Default.SetAsync(key, value);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SecureStorage] SetAsync kalıcı hata: {ex.Message}");
            _usePreferencesFallback = true;
            await CleanupAllKeysAsync();
            Preferences.Default.Set(key, value);
            return true;
        }
    }

    public static void Remove(string key)
    {
        if (_usePreferencesFallback)
        {
            Preferences.Default.Remove(key);
            return;
        }

        try
        {
            SecureStorage.Default.Remove(key);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SecureStorage] Remove kalıcı hata: {ex.Message}");
            _usePreferencesFallback = true;
            Preferences.Default.Remove(key);
        }
    }

    public static async Task CleanupAllKeysAsync()
    {
        try
        {
            foreach (var key in new[] { KeyUserId, KeySavedEmail, KeySavedPassword })
            {
                SecureStorage.Default.Remove(key);
                Preferences.Default.Remove(key);
            }
        }
        catch { /* ignore */ }

#if ANDROID
        try
        {
            var keyStore = KeyStore.GetInstance("AndroidKeyStore");
            keyStore.Load(null);
            var aliases = keyStore.Aliases();
            while (aliases.HasMoreElements)
            {
                var alias = aliases.NextElement().ToString();
                if (alias.Contains(global::Android.App.Application.Context.PackageName))
                {
                    keyStore.DeleteEntry(alias);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SecureStorage] KeyStore temizleme başarısız: {ex.Message}");
        }
#endif
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

    public static Task CleanupCorruptedKeysAsync() => CleanupAllKeysAsync();
}