using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OtoServisApp.Services;

namespace OtoServisApp.Services
{
    public static class ApiConfig
    {
        // Çalışan platforma göre doğru API adresini otomatik seçer
        public static string BaseUrl
        {
            get
            {
                // Eğer gerçek sunucuyu test etmek istiyorsan direkt bulut IP'sini veriyoruz
                return "http://136.115.53.49:8000";

                /* //Geliştirme aşamasında local kullanmak istersen eski blok burada durabilir:
                #if ANDROID
                    return "http://10.0.2.2:8000";
                #else
                    return "http://127.0.0.1:8000";
                #endif
                */
            }
        }
    }
}
