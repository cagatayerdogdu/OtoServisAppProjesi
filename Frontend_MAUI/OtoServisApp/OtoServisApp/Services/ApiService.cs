using System.Net.Http.Json;
using System.Text.Json;
using OtoServisApp.Models;
using Plugin.Firebase.CloudMessaging;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace OtoServisApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            // ApiConfig sınıfımızdan doğru IP adresini (127.0.0.1 veya 10.0.2.2) alıyoruz.
            //_httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);

            // ApiConfig dosyasını tamamen eziyoruz ve telefonun hedefini doğrudan senin bilgisayarına (192.168.0.13) kilitliyoruz:
            //_httpClient.BaseAddress = new Uri("http://192.168.0.13:8000/");

            // Tünel sayesinde tekrar localhost'a dönüyoruz
            //_httpClient.BaseAddress = new Uri("http://127.0.0.1:8000/");

            // Tüm VPN, Güvenlik Duvarı ve Yerel Ağ sorunlarını ezip geçen Ngrok tünelimiz:
            //_httpClient.BaseAddress = new Uri("https://runny-scrutinizingly-ela.ngrok-free.dev/");

            // REVİZE: Eski Ngrok tüneli yerine Google Cloud Sabit IP adresimizi kullanıyoruz.
            // Artık tünele gerek yok, doğrudan sunucuya bağlanıyoruz.
            _httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);
        }
        /*
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
        */
        public async Task<Kullanici> GirisYapAsync(string eposta, string sifre)
        {
            try
            {
                // Senin orijinal yapın: JSON olarak veri gönderimi
                var loginData = new { eposta = eposta, sifre = sifre };
                var response = await _httpClient.PostAsJsonAsync("/giris/", loginData).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonSerializer.Deserialize<Kullanici>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    // --- YENİ REVİZE: Hata detayını ayrıştırıp fırlatma (Madde 73) ---
                    string rawError = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    string temizMesaj = "Sunucu ile iletişim kurulamadı.";

                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(rawError);
                        var root = jsonDoc.RootElement;

                        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("detail", out var detailElement))
                        {
                            temizMesaj = detailElement.ValueKind == JsonValueKind.String
                                ? detailElement.GetString()
                                : detailElement.ToString();
                        }
                    }
                    catch
                    {
                        temizMesaj = rawError; // JSON okunamıyorsa düz metni ver
                    }

                    throw new Exception(temizMesaj);
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"HTTP Hatası: {httpEx.Message}");
                throw new Exception($"Sunucuya ulaşılamıyor. Lütfen internet bağlantınızı kontrol edin. Hata: {httpEx.Message}\");");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Giriş hatası: {ex.Message}");
                throw; // UI (LoginView) tarafında catch bloğuna düşmesi için hatayı fırlatıyoruz
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
                //if (response.IsSuccessStatusCode) return "OK";
                // YENİ REVİZE: Bize ID lazım ki fotoğrafı o ID'ye bağlayalım
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using var jsonDoc = JsonDocument.Parse(content);
                    return jsonDoc.RootElement.GetProperty("id").GetInt32().ToString(); // Başarılıysa ID döner
                }

                // API'den dönen ham yanıtı (JSON) okuyoruz
                string rawError = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string temizMesaj = rawError; // Parse edilemezse varsayılan olarak ham yanıt dönsün

                try
                {
                    // Gelen JSON paketini açıyoruz
                    using var jsonDoc = JsonDocument.Parse(rawError);
                    var root = jsonDoc.RootElement;

                    // 1. Durum: Standart FastAPI Hatası -> {"detail": "Bu araç için..."}
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("detail", out var detailElement))
                    {
                        if (detailElement.ValueKind == JsonValueKind.String)
                        {
                            temizMesaj = detailElement.GetString();
                        }
                        // Eğer detail'in kendisi köşeli parantezli bir dizi ise
                        else if (detailElement.ValueKind == JsonValueKind.Array && detailElement.GetArrayLength() > 0)
                        {
                            temizMesaj = detailElement[0].TryGetProperty("msg", out var msgProp) ? msgProp.GetString() : detailElement.ToString();
                        }
                    }
                    // 2. Durum: Doğrudan Dizi Olarak Gelen Hatalar (Validation) -> [{"msg": "...", ...}]
                    else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var firstError = root[0];
                        if (firstError.TryGetProperty("msg", out var msgProp))
                        {
                            temizMesaj = msgProp.GetString();
                        }
                    }
                }
                catch
                {
                    // Gelen yanıt JSON değilse (örneğin düz HTML 500 sayfasıysa) hiçbir şey yapma, ham hali kalsın
                }

                return temizMesaj;
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

        /*public async Task<string> KullaniciKayitAsync(object yeniKullanici)
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
        }*/

        /************** OTP DOĞRULAMA İLE KULLANCI KAYDET **************/
        /// <summary>
        /// Belirtilen e-posta adresine doğrulama kodu gönderir.
        /// </summary>
        public async Task<bool> EpostaDogrulamaKoduGonderAsync(string eposta)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("eposta", eposta)
        });
                var response = await _httpClient.PostAsync("kayit/eposta-dogrulama-kodu", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Doğrulama kodunu kontrol eder ve kullanıcı kaydını tamamlar.
        /// Başarılı olursa "OK", aksi takdirde hata mesajı döner.
        /// </summary>
        public async Task<string> DogrulaVeKaydetAsync(string adSoyad, string telefon, string eposta, string sifre, bool mailIstiyorMu, string dogrulamaKodu)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("ad_soyad", adSoyad),
            new KeyValuePair<string, string>("telefon", telefon),
            new KeyValuePair<string, string>("eposta", eposta),
            new KeyValuePair<string, string>("sifre", sifre),
            new KeyValuePair<string, string>("mail_istiyor_mu", mailIstiyorMu.ToString()),
            new KeyValuePair<string, string>("dogrulama_kodu", dogrulamaKodu)
        });

                var response = await _httpClient.PostAsync("kayit/dogrula-ve-kaydet", content);

                if (response.IsSuccessStatusCode)
                    return "OK";

                var error = await response.Content.ReadAsStringAsync();
                return error;
            }
            catch (Exception ex)
            {
                return $"Bağlantı hatası: {ex.Message}";
            }
        }
        /******************************/
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
                Debug.WriteLine($"API HATASI: {ex.Message}");
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

        // Parametreye islem_yapan_id eklendi (Opsiyonel olarak)
        public async Task<bool> AdminTalepGuncelleAsync(int talepId, string yeniDurum, double tutar, int? islem_yapan_id = null)
        {
            try
            {
                var payload = new 
                { 
                    yeni_durum = yeniDurum,
                    tahmini_tutar = tutar,
                    islem_yapan_id = islem_yapan_id 
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsJsonAsync($"/admin/servis-talepleri/{talepId}/guncelle", payload).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Güncelleme hatası: {ex.Message}");
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
                Debug.WriteLine($"API HATASI: {ex.Message}");
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
                Debug.WriteLine($"API HATASI: {ex.Message}");
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
                Debug.WriteLine($"API HATASI: {ex.Message}");
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
                    await ModernAlertService.ShowInfoAsync(ex.Message, "Log Çekme Hatası");
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
                    return await Task.Run(() => JsonSerializer.Deserialize<List<BildirimResponse>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<BildirimResponse>());
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

        // DeepSeek Bildirimleri sayfalı getirme Lazy Loading
        public async Task<(List<BildirimResponse> bildirimler, int toplamKayit)> BildirimleriSayfaliGetirAsync(
                    int kullaniciId,
                    int skip = 0,
                    int limit = 20)
        {
            try
            {
                string url = $"/bildirimler/{kullaniciId}/sayfali?skip={skip}&limit={limit}";
                var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonSerializer.Deserialize<SayfaliBildirimResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return (result.bildirimler ?? new List<BildirimResponse>(), result.toplam_kayit);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sayfalı bildirim hatası: {ex.Message}");
            }
            return (new List<BildirimResponse>(), 0);
        }

        // Yardımcı sınıf
        public class SayfaliBildirimResponse
        {
            public List<BildirimResponse> bildirimler { get; set; }
            public int toplam_kayit { get; set; }
        }
        // DeepSeek Bildirimleri sayfalı getirme Lazy Loading Bitişi

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
                            await ModernAlertService.ShowInfoAsync("Token telefondan alındı ama Python API'ye ulaşıp kaydedilemedi!", "API Hatası");
                        }
                    });
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await ModernAlertService.ShowInfoAsync("Google cihazınız için Token üretemedi (Boş döndü).", "Cihaz Hatası");
                    });
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ModernAlertService.ShowInfoAsync($"FCM Çöktü: {ex.Message}", "Sistem Hatası");
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

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            // Eğer token ekleme mantığı varsa buraya dahil olur, yoksa direkt atar
            return await _httpClient.GetAsync(endpoint);
        }

        public async Task<HttpResponseMessage> PutAsync(string endpoint, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PutAsync(endpoint, content);
        }

        // Bu metot, CRM ekranından atacağımız manuel hatırlatmalar için gereklidir.
        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync(endpoint, content);
        }
        
        //Madde 50: Bildirimleri Toplu/Tekli Silme (Swipe to Delete)
        public async Task<bool> NotificationsDeleteAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //Madde 37: Pasif Kullanıcı Diriltme
        public async Task<Kullanici> PasifKullaniciSorgulaAsync(string email)
        {
            try
            {
                // Python tarafındaki /kullanicilar/pasif/{email} ucuna istek atar
                var response = await _httpClient.GetAsync($"kullanicilar/pasif/{email}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Kullanici>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Pasif sorgulama hatası: {ex.Message}");
            }
            return null;
        }
        // Madde 72-Servis talebi ekranında aracın hasar fotolarını yükleyebilmesi gerekiyor.
        public async Task<string> UploadHasarFotografAsync(int talepId, Stream fileStream, string fileName)
        {
            try
            {
                using var multipartFormContent = new MultipartFormDataContent();

                // YENİ REVİZE: Dosya yolu yerine Stream (Akış) kullanıyoruz, Android erişim engeli kalkıyor!
                var fileStreamContent = new StreamContent(fileStream);
                fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                multipartFormContent.Add(fileStreamContent, name: "file", fileName: fileName);

                var response = await _httpClient.PostAsync($"/servis-talepleri/{talepId}/fotograf", multipartFormContent).ConfigureAwait(false);

                if (response.IsSuccessStatusCode) return "OK";

                // Hata mesajını ayrıştır (Dizi [array] olarak gelen 422 hatalarını da yakalar)
                string rawError = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string temizMesaj = "Sunucu fotoğrafı kabul etmedi.";
                try
                {
                    using var jsonDoc = JsonDocument.Parse(rawError);
                    var root = jsonDoc.RootElement;
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("detail", out var detailElement))
                    {
                        if (detailElement.ValueKind == JsonValueKind.String)
                            temizMesaj = detailElement.GetString();
                        else if (detailElement.ValueKind == JsonValueKind.Array && detailElement.GetArrayLength() > 0)
                            temizMesaj = detailElement[0].TryGetProperty("msg", out var msgProp) ? msgProp.GetString() : detailElement.ToString();
                    }
                }
                catch { }

                return temizMesaj;
            }
            catch (Exception ex)
            {
                return $"Bağlantı Hatası: Fotoğraf yüklenemedi. ({ex.Message})";
            }
        }

        // Düzenleme ekranında yeni fotoğraflar yüklenmeden önce eskileri temizler
        public async Task<bool> EskiFotograflariTemizleAsync(int talepId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/servis-talepleri/{talepId}/fotograflari-temizle").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        // Python'a yazdığımız API ucuna bağlanmak için:
        public async Task<List<ServisTalebiFotograf>> TalepFotograflariniGetirAsync(int talepId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ServisTalebiFotograf>>($"/servis-talepleri/{talepId}/fotograflar").ConfigureAwait(false) ?? new List<ServisTalebiFotograf>();
            }
            catch
            {
                return new List<ServisTalebiFotograf>();
            }
        }

        // YENİ REVİZE: Tek bir fotoğrafı silme metodu
        public async Task<bool> FotografSilAsync(int fotoId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/fotograflar/{fotoId}").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<int, bool>> TopluFotografDurumuGetirAsync(List<int> talepIdleri)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { talep_idleri = talepIdleri });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"/servis-talepleri/toplu-fotograf-durumu", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Dictionary<int, bool>>(jsonResponse);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Toplu fotoğraf durumu alınamadı: {ex.Message}");
            }
            return new Dictionary<int, bool>();
        }

        /* Sayfalı ve Filtreli Metot (Kullanıcı Tarafı) - DeepSeek */
        /// <summary>
        /// Kullanıcının servis taleplerini sayfalı ve filtreli olarak getirir.
        /// </summary>
        public async Task<(List<ServisTalebi> talepler, int toplamKayit)> KullaniciTalepleriniSayfaliGetirAsync(
            int kullaniciId,
            int skip = 0,
            int limit = 20,
            string durum = null,
            string arama = null)
        {
            try
            {
                var queryParams = new List<string>
        {
            $"skip={skip}",
            $"limit={limit}"
        };
                if (!string.IsNullOrEmpty(durum) && durum != "Tümü")
                    queryParams.Add($"durum={Uri.EscapeDataString(durum)}");
                if (!string.IsNullOrEmpty(arama))
                    queryParams.Add($"arama={Uri.EscapeDataString(arama)}");

                string url = $"/servis-talepleri/kullanici/{kullaniciId}?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonSerializer.Deserialize<SayfaliTaleplerResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return (result.talepler ?? new List<ServisTalebi>(), result.toplam_kayit);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sayfalı talep çekme hatası: {ex.Message}");
            }
            return (new List<ServisTalebi>(), 0);
        }
        /* Sayfalı ve Filtreli Metot (Admin Tarafı) - DeepSeek */
        /// <summary>
        /// Admin paneli için talepleri sayfalı ve filtreli olarak getirir.
        /// </summary>
        public async Task<(List<ServisTalebi> talepler, int toplamKayit)> AdminTalepleriniSayfaliGetirAsync(
            int skip = 0,
            int limit = 20,
            string durum = null,
            string arama = null)
        {
            try
            {
                var queryParams = new List<string>
        {
            $"skip={skip}",
            $"limit={limit}"
        };
                if (!string.IsNullOrEmpty(durum) && durum != "Tümü")
                    queryParams.Add($"durum={Uri.EscapeDataString(durum)}");
                if (!string.IsNullOrEmpty(arama))
                    queryParams.Add($"arama={Uri.EscapeDataString(arama)}");

                string url = $"/admin/servis-talepleri/sayfali?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonSerializer.Deserialize<SayfaliAdminTaleplerResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return (result.talepler ?? new List<ServisTalebi>(), result.toplam_kayit);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Admin sayfalı talep hatası: {ex.Message}");
            }
            return (new List<ServisTalebi>(), 0);
        }

        public async Task<(List<ServisTalebi> talepler, int toplamKayit)> AdminGecmisTalepleriSayfaliGetirAsync(
                    int skip = 0,
                    int limit = 20,
                    string durum = null,
                    string arama = null
            )
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"skip={skip}",
                    $"limit={limit}"
                };
                if (!string.IsNullOrEmpty(durum) && durum != "Tümü")
                    queryParams.Add($"durum={Uri.EscapeDataString(durum)}");
                if (!string.IsNullOrEmpty(arama))
                    queryParams.Add($"arama={Uri.EscapeDataString(arama)}");

                string url = $"/admin/servis-talepleri/gecmis?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonSerializer.Deserialize<SayfaliTaleplerResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return (result.talepler ?? new List<ServisTalebi>(), result.toplam_kayit);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Geçmiş talepler sayfalı çekme hatası: {ex.Message}");
            }
            return (new List<ServisTalebi>(), 0);
        }

        // Yardımcı sınıf
        public class SayfaliAdminTaleplerResponse
        {
            public List<ServisTalebi> talepler { get; set; }
            public int toplam_kayit { get; set; }
        }

        /*********************************/
        /****** Vitrinimiz Başlangıç *****/
        /*********************************/
        public async Task<List<TamamlananIs>> VitrinListesiGetirAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TamamlananIs>>("/vitrin") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<TamamlananIs> VitrinEkleAsync(string baslik, string aciklama, string etiket, string tarih, int? hizmetId, Stream fotoStream, string fotoDosyaAdi)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(baslik), "baslik");
            content.Add(new StringContent(aciklama), "aciklama");
            content.Add(new StringContent(etiket), "etiket");
            content.Add(new StringContent(tarih), "tarih");
            if (hizmetId.HasValue)
                content.Add(new StringContent(hizmetId.Value.ToString()), "hizmet_id");

            var streamContent = new StreamContent(fotoStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(streamContent, "file", fotoDosyaAdi);

            var response = await _httpClient.PostAsync("/admin/vitrin", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TamamlananIs>();
        }

        public async Task<TamamlananIs> VitrinGuncelleAsync(int id, string baslik, string aciklama, string etiket, string tarih, int? hizmetId, Stream? fotoStream = null, string? fotoDosyaAdi = null)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(baslik), "baslik");
            content.Add(new StringContent(aciklama), "aciklama");
            content.Add(new StringContent(etiket), "etiket");
            content.Add(new StringContent(tarih), "tarih");
            if (hizmetId.HasValue)
                content.Add(new StringContent(hizmetId.Value.ToString()), "hizmet_id");

            if (fotoStream != null && !string.IsNullOrEmpty(fotoDosyaAdi))
            {
                var streamContent = new StreamContent(fotoStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "file", fotoDosyaAdi);
            }

            var response = await _httpClient.PutAsync($"/admin/vitrin/{id}", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TamamlananIs>();
        }

        public async Task<bool> VitrinSilAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"/admin/vitrin/{id}");
            return response.IsSuccessStatusCode;
        }

        /****** Vitrinimiz Bitişi *****/

        /// <summary>
        /// İstanbul'un ilçelerini getirir.
        /// </summary>
        public async Task<List<District>> IlceleriGetirAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("adres/ilceler");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<TurkiyeApiProvinceResponse>(content, options);
                    return result?.Data?.Districts ?? new List<District>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"İlçeler alınamadı: {ex.Message}");
            }
            return new List<District>();
        }

        /* İstanbul içi Adresler endpointi metodu */
        /// <summary>
        /// Kullanıcının adres bilgisini kaydeder.
        /// </summary>
        public async Task<string> AdresKaydetAsync(int kullaniciId, string adSoyad, string ilce, string mahalle, string sokak, string no)
        {
            try
            {
                var payload = new
                {
                    kullanici_id = kullaniciId,
                    ad_soyad = adSoyad,
                    ilce = ilce,
                    mahalle = mahalle,
                    sokak = sokak,
                    no = no
                };
                var response = await _httpClient.PostAsJsonAsync("adres/kaydet", payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AdresKayitResponse>();
                    return result?.TamAdres;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Adres kaydedilemedi: {ex.Message}");
            }
            return null;
        }

        // Yardımcı sınıf
        public class AdresKayitResponse
        {
            [JsonPropertyName("tam_adres")]
            public string TamAdres { get; set; }
        }

    } /* En Dıştaki public class ApiService bitişi */

    // Yardımcı sınıf (ApiService.cs içinde aynı dosyaya ekleyin)
    public class SayfaliTaleplerResponse
    {
        public List<ServisTalebi> talepler { get; set; }
        public int toplam_kayit { get; set; }
    }
}