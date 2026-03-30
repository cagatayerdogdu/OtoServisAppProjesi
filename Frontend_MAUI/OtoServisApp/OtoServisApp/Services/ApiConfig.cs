using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtoServisApp.Services
{
    public static class ApiConfig
    {
        // Çalışan platforma göre doğru API adresini otomatik seçer
        public static string BaseUrl
        {
            get
            {
#if ANDROID
                return "http://10.0.2.2:8000"; // Android Emulator'ün bilgisayara çıkış IP'si
#else
                return "http://127.0.0.1:8000"; // Windows veya iOS (Local)
#endif
            }
        }
    }
}
