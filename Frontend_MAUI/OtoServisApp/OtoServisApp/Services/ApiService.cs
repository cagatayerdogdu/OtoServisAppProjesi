using System.Net.Http.Json;
using System.Text.Json;
using OtoServisApp.Models;
using Plugin.Firebase.CloudMessaging;

namespace OtoServisApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            // ApiConfig sınıfımızdan doğru IP adresini (127.0.0.1 veya 10.0.2.2) alıyoruz
            //_httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);

            // ApiConfig dosyasını tamamen eziyoruz ve telefonun hedefini doğrudan senin bilgisayarına (192.168.0.13) kilitliyoruz:
            //_httpClient.BaseAddress = new Uri("http://192.168.0.13:8000/");

            // Tünel sayesinde tekrar localhost'a dönüyoruz
            //_httpClient.BaseAddress = new Uri("http://127.0.0.1:8000/");

            // Tüm VPN, Güvenlik Duvarı ve Yerel Ağ sorunlarını ezip geçen Ngrok tünelimiz:
            _httpClient.BaseAddress = new Uri("https://runny-scrutinizingly-ela.ngrok-free.dev/");
        }

        public async Task<Kullanici> GirisYapAsync(string eposta, string sifre)
        {
            try
            {
                var loginData = new LoginRequest { eposta = eposta, sifre = sifre };
                var response = await _httpClient.PostAsJsonAsync("/giris/", loginData).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Kullanici>().ConfigureAwait(false);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Hatası: {ex.Message}");
                return null;
            }
        }

        public async Task<Kullanici> KullaniciGuncelleAsync(int id, KullaniciUpdate guncelVeri)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/kullanicilar/{id}", guncelVeri).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Kullanici>().ConfigureAwait(false);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Hatası: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Marka>> MarkalariGetirAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Marka>>("/referanslar/markalar/").ConfigureAwait(false) ?? new List<Marka>();
            }
            catch
            {
                return new List<Marka>();
            }
        }

        public async Task<List<AracModel>> ModelleriGetirAsync(int markaId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/referanslar/modeller/{markaId}").ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    response = await _httpClient.GetAsync($"/referanslar/modeller/?marka_id={markaId}").ConfigureAwait(false);
                }

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<AracModel>>().ConfigureAwait(false) ?? new List<AracModel>();
                }
                return new List<AracModel>();
            }
            catch
            {
                return new List<AracModel>();
            }
        }

        public async Task<Arac> AracEkleAsync(Arac yeniArac)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/araclar/", yeniArac).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Arac>().ConfigureAwait(false);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Hatası: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Hizmet>> HizmetleriGetirAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Hizmet>>("/referanslar/hizmetler/").ConfigureAwait(false) ?? new List<Hizmet>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hizmetleri Çekerken Hata: {ex.Message}");
                return new List<Hizmet>();
            }
        }

        public async Task<string> ServisTalebiOlusturAsync(ServisTalebiRequest talep)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/servis-talepleri/", talep).ConfigureAwait(false);
                if (response.IsSuccessStatusCode) return "OK";

                string errorDetail = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return $"Sunucu Hatası: {errorDetail}";
            }
            catch (Exception ex)
            {
                return $"Bağlantı Hatası: {ex.Message}\nÇözüm: İnternet bağlantınızı ve API sunucusunu kontrol edin.";
            }
        }

        public async Task<bool> AracGuncelleAsync(int aracId, Arac guncelArac)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/araclar/{aracId}", guncelArac).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Araç Güncelleme Hatası: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AracSilAsync(int aracId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/araclar/{aracId}").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Araç Silme Hatası: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ServisTalebi>> ServisTalepleriniGetirAsync(int kullaniciId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ServisTalebi>>($"/servis-talepleri/kullanici/{kullaniciId}").ConfigureAwait(false) ?? new List<ServisTalebi>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Talepleri Çekerken Hata: {ex.Message}");
                return new List<ServisTalebi>();
            }
        }

        public async Task<bool> ServisTalebiSilAsync(int talepId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/servis-talepleri/{talepId}").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Çift isimlendirme karışıklığını çözen köprü metot:
        public async Task<List<Arac>> KullaniciAraclariGetirAsync(int kullaniciId) => await KullaniciAraclariniGetirAsync(kullaniciId).ConfigureAwait(false);

        public async Task<List<Arac>> KullaniciAraclariniGetirAsync(int kullaniciId)
        {
            try
            {
                var guncelKullanici = await _httpClient.GetFromJsonAsync<Kullanici>($"/kullanicilar/{kullaniciId}").ConfigureAwait(false);
                return guncelKullanici?.araclar ?? new List<Arac>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Araçları Çekerken Hata: {ex.Message}");
                return new List<Arac>();
            }
        }

        public async Task<string> KullaniciKayitAsync(object yeniKullanici)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/kullanicilar/", yeniKullanici).ConfigureAwait(false);
                if (response.IsSuccessStatusCode) return "OK";

                return $"Kayıt Başarısız: Bu e-posta zaten sistemde kayıtlı olabilir.";
            }
            catch (Exception ex)
            {
                return $"Bağlantı Hatası: Sunucuya ulaşılamadı. ({ex.Message})";
            }
        }

        public async Task<string> SifreSifirlamaTalepEtAsync(string eposta)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/kullanicilar/sifre-sifirla", new { eposta = eposta }).ConfigureAwait(false);
                if (response.IsSuccessStatusCode) return "OK";

                return "Bu e-posta adresine ait bir hesap bulunamadı.";
            }
            catch
            {
                return "Bağlantı hatası. Lütfen internetinizi kontrol edin.";
            }
        }

        public async Task<bool> YeniSifreKaydetAsync(string eposta, string yeniSifre)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/kullanicilar/yeni-sifre-kaydet", new { eposta = eposta, yeni_sifre = yeniSifre }).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ServisTalebiGuncelleAsync(int talepId, int? hizmetId, int? aracId, string talepTarihi, string adres, string notlar, bool duzeltmeIstendiMi, string duzeltmeNotu)
        {
            try
            {
                var payload = new
                {
                    hizmet_id = hizmetId,
                    arac_id = aracId,
                    talep_tarihi = talepTarihi,
                    adres = adres,
                    notlar = notlar,
                    duzeltme_istendi_mi = duzeltmeIstendiMi,
                    duzeltme_notu = duzeltmeNotu
                };
                var response = await _httpClient.PutAsJsonAsync($"/servis-talepleri/{talepId}", payload).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API HATASI: {ex.Message}");
                return false;
            }
        }

        public async Task<Arac> AracGetirAsync(int aracId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Arac>($"/araclar/{aracId}").ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> SifreDegistirAsync(int kullaniciId, string eskiSifre, string yeniSifre)
        {
            try
            {
                var payload = new { kullanici_id = kullaniciId, eski_sifre = eskiSifre, yeni_sifre = yeniSifre };
                var response = await _httpClient.PostAsJsonAsync("/kullanicilar/sifre-degistir", payload).ConfigureAwait(false);
                if (response.IsSuccessStatusCode) return "OK";

                return "Hata: Mevcut şifrenizi yanlış girdiniz.";
            }
            catch
            {
                return "Bağlantı hatası oluştu.";
            }
        }

        public async Task<List<ServisTalebi>> AdminTumTalepleriniGetirAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ServisTalebi>>("/admin/servis-talepleri").ConfigureAwait(false) ?? new List<ServisTalebi>();
            }
            catch
            {
                return new List<ServisTalebi>();
            }
        }

        public async Task<bool> AdminTalepGuncelleAsync(int talepId, string yeniDurum, double tutar)
        {
            try
            {
                var payload = new { yeni_durum = yeniDurum, tahmini_tutar = tutar };
                var response = await _httpClient.PutAsJsonAsync($"/admin/servis-talepleri/{talepId}/guncelle", payload).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ServisTalebi>> AdminAktifTalepleriGetirAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ServisTalebi>>("/admin/servis-talepleri/aktif").ConfigureAwait(false) ?? new List<ServisTalebi>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API HATASI: {ex.Message}");
                return new List<ServisTalebi>();
            }
        }

        public async Task<List<ServisTalebi>> AdminGecmisTalepleriGetirAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ServisTalebi>>("/admin/servis-talepleri/gecmis").ConfigureAwait(false) ?? new List<ServisTalebi>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API HATASI: {ex.Message}");
                return new List<ServisTalebi>();
            }
        }

        public async Task<bool> KullaniciSilAsync(int kullaniciId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/kullanicilar/{kullaniciId}").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API HATASI: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AracAktifTalepVarMiAsync(int aracId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/araclar/{aracId}/aktif-talep-kontrol").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var sonuc = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>().ConfigureAwait(false);
                    if (sonuc != null && sonuc.ContainsKey("aktif_talep_var"))
                    {
                        return sonuc["aktif_talep_var"];
                    }
                }
            }
            catch { }
            return false;
        }

        public async Task<LogResponse> AdminLoglariGetirAsync(string seviye = null, DateTime? baslangic = null, DateTime? bitis = null, int sayfa = 1, int limit = 50)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(seviye) && seviye != "Tümü") queryParams.Add($"seviye={Uri.EscapeDataString(seviye)}");
                if (baslangic.HasValue) queryParams.Add($"baslangic_tarihi={baslangic.Value.ToString("yyyy-MM-dd")}");
                if (bitis.HasValue) queryParams.Add($"bitis_tarihi={bitis.Value.ToString("yyyy-MM-dd")}");
                queryParams.Add($"sayfa={sayfa}");
                queryParams.Add($"limit={limit}");

                string url = "admin/loglar/";
                if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

                return await _httpClient.GetFromJsonAsync<LogResponse>(url).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await App.Current.MainPage.DisplayAlert("Log Çekme Hatası", ex.Message, "Tamam");
                });
                return null;
            }
        }

        public async Task<List<BildirimResponse>> KullaniciBildirimleriniGetirAsync(int kullaniciId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"bildirimler/{kullaniciId}").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonSerializer.Deserialize<List<BildirimResponse>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<BildirimResponse>();
                }
                return new List<BildirimResponse>();
            }
            catch
            {
                return new List<BildirimResponse>();
            }
        }

        public async Task<bool> BildirimOkunduIsaretleAsync(int bildirim_id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"bildirimler/{bildirim_id}/okundu", null).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> OkunmamisBildirimSayisiGetirAsync(int kullaniciId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"bildirimler/{kullaniciId}/okunmamis-sayi").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (int.TryParse(json, out int sayi)) return sayi;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task FcmTokenGuncelle(int kullaniciId)
        {
#if ANDROID || IOS
            try
            {
                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync().ConfigureAwait(false);
                var fcmToken = await CrossFirebaseCloudMessaging.Current.GetTokenAsync().ConfigureAwait(false);

                if (!string.IsNullOrEmpty(fcmToken))
                {
                    var basarili = await KullaniciTokenKaydetAsync(kullaniciId, fcmToken).ConfigureAwait(false);

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (basarili)
                        {
                            //await Application.Current.MainPage.DisplayAlert("Başarılı", "FCM Token alındı ve veritabanına yazıldı!", "Tamam");

                            // Sessizce arka planda kaydeder, kullanıcıyı rahatsız etmez
                            await KullaniciTokenKaydetAsync(kullaniciId, fcmToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("API Hatası", "Token telefondan alındı ama Python API'ye ulaşıp kaydedilemedi!", "Tamam");
                        }
                    });
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert("Cihaz Hatası", "Google cihazınız için Token üretemedi (Boş döndü).", "Tamam");
                    });
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Sistem Hatası", $"FCM Çöktü: {ex.Message}", "Tamam");
                });
            }
#else
            Console.WriteLine("Firebase bildirimleri Windows ortamında desteklenmiyor.");
            await Task.CompletedTask;
#endif
        }

        public async Task<bool> KullaniciTokenKaydetAsync(int kullaniciId, string token)
        {
            try
            {
                var payload = new { kullanici_id = kullaniciId, fcm_token = token };
                var response = await _httpClient.PostAsJsonAsync("kullanici/token-kaydet/", payload).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token Kayıt Hatası API: {ex.Message}");
                return false;
            }
        }
    }
}