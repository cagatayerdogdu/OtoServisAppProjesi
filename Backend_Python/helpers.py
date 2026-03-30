# helpers.py

class FiyatlandirmaMotoru:
    """
    Şimdilik statik değişkenlerle çalışan, ileride Yönetim Paneli üzerinden 
    veritabanına bağlanacak olan fiyat hesaplama sınıfı.
    """
    
    TABAN_FIYAT = 500  # Standart arıza tespit / servis giriş ücreti
    VALE_UCRETI = 300  # Kapıdan alma hizmet bedeli. Burası ileride km.ye (konuma) bağlı olarak ayarlanmalı.
    
    # Lüks araçlar için parça/işçilik çarpanları
    MARKA_CARPANLARI = {
        "BMW": 1.5,        # %50 daha pahalı
        "Mercedes": 1.6,
        "Fiat": 1.0,       # Standart fiyat
        "Ford": 1.1,
        "Renault": 1.0,
        "Volkswagen": 1.2,
        "Toyota": 1.1
    }

    @classmethod
    def tahmini_fiyat_hesapla(cls, marka_adi: str, vale_istendi_mi: bool) -> int:
        # 1. Taban fiyatla başla
        fiyat = cls.TABAN_FIYAT
        
        # 2. Marka çarpanını uygula (Eğer marka listede yoksa 1.0 olarak al)
        carpan = cls.MARKA_CARPANLARI.get(marka_adi, 1.0)
        fiyat = fiyat * carpan
        
        # 3. Vale istendiyse ücreti ekle
        if vale_istendi_mi:
            fiyat += cls.VALE_UCRETI
            
        return int(fiyat)