from sqlalchemy import Column, Integer, String, Boolean, ForeignKey, Text, Numeric, DateTime, Float, case
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func # Tarih işlemleri için eklendi
from database import Base
import datetime
from sqlalchemy import Date



# --- REFERANS TABLOLARI ---
class Marka(Base):
    __tablename__ = "markalar"
    __table_args__ = {'comment': 'Sistemde tanimli olan arac markalarinin tutuldugu referans tablosu (Orn: Ford, BMW).'}

    id = Column(Integer, primary_key=True, index=True, comment="Marka tekil kimligi (PK)")
    ad = Column(String(100), unique=True, index=True, comment="Marka adi")
    
    modeller = relationship("Model", back_populates="marka")

class Model(Base):
    __tablename__ = "modeller"
    __table_args__ = {'comment': 'Markalara ait arac modellerinin tutuldugu referans tablosu (Orn: Focus, 3.20i).'}

    id = Column(Integer, primary_key=True, index=True, comment="Model tekil kimligi (PK)")
    marka_id = Column(Integer, ForeignKey("markalar.id"), comment="Bagli oldugu markanin ID'si (FK)")
    ad = Column(String(100), index=True, comment="Model adi")
    
    marka = relationship("Marka", back_populates="modeller")

# --- ANA TABLOLAR ---
class Kullanici(Base):
    __tablename__ = "kullanicilar"
    __table_args__ = {'comment': 'Sisteme kayitli musteri ve yoneticilerin bilgilerini tutan ana tablo.'}

    id = Column(Integer, primary_key=True, index=True, comment="Kullanici tekil kimligi (PK)")
    ad_soyad = Column(String(100), comment="Kullanicinin tam adi")
    eposta = Column(String(100), unique=True, index=True, comment="Giris ve iletisim icin e-posta adresi")
    telefon = Column(String(20), unique=True, comment="Iletisim icin telefon numarasi")
    adres = Column(Text, nullable=True, comment="Kullanicinin kayitli adresi (Opsiyonel)") # YENİ EKLENDİ
    sifre_hash = Column(String(255), comment="Guvenlik icin hashlenmis sifre")
    rol = Column(String(20), default="Musteri", comment="Musteri veya Admin")
    fcm_token = Column(String(255), nullable=True, comment="Firebase Push Notification Token bilgisi")
    # Müşteri Takip ve KVKK Kolonları
    son_giris_tarihi = Column(DateTime, nullable=True, comment="Kullanıcının sisteme son giriş yaptığı tarih")
    mail_istiyor_mu = Column(Boolean, default=True, comment="KVKK Kapsamında mail alma izni")
    son_hatirlatma_tarihi = Column(DateTime, nullable=True, comment="Her gün mail atıp spamlememek için son hatırlatma zamanı")
    
    # Denetim (Audit) Kolonları - Raporlama ve ETL için kritik
    aktif_mi = Column(Boolean, default=True, comment="Kullanici hesabi aktif mi? (Gecmisi silmemek / soft-delete icin)")
    kayit_tarihi = Column(DateTime, default=datetime.datetime.utcnow, comment="Hesabin olusturulma zamani")
    
    # araclar = relationship("Arac", back_populates="sahip")
    servis_talepleri = relationship("ServisTalebi", foreign_keys="[ServisTalebi.kullanici_id]", back_populates="musteri")
    araclar = relationship("Arac")
    # servis_talepleri = relationship("ServisTalebi")
    
    # BÜTÜN ANA TABLOLARIN (Kullanici, Arac, ServisTalebi) EN ALTINA ŞU İKİ SATIRI EKLE:
    # kayit_durumu = Column(String(1), default="A", comment="A: Aktif Kayit, X: Silinmis (Soft Delete)") aktif_mi kolonundan zaten takip ediyormuşuz.
    silinme_tarihi = Column(DateTime, nullable=True, comment="Kaydin X durumuna cekildigi tarih")

class Arac(Base):
    __tablename__ = "araclar"
    __table_args__ = {'comment': 'Musterilere ait araclarin donanim, marka ve model bilgilerinin tutuldugu tablo.'}

    id = Column(Integer, primary_key=True, index=True, comment="Arac tekil kimligi (PK)")
    sahip_id = Column(Integer, ForeignKey("kullanicilar.id"), comment="Aracin sahibinin ID'si (FK)")
    
    marka_id = Column(Integer, ForeignKey("markalar.id"), nullable=True, comment="Sistemden secilen marka ID'si (FK)")
    model_id = Column(Integer, ForeignKey("modeller.id"), nullable=True, comment="Sistemden secilen model ID'si (FK)")
    
    ozel_marka = Column(String(100), nullable=True, comment="Eger marka listede yoksa kullanicinin manuel girdigi marka")
    ozel_model = Column(String(100), nullable=True, comment="Eger model listede yoksa kullanicinin manuel girdigi model")
    
    yil = Column(Integer, comment="Aracin uretim yili")
    yakit_tipi = Column(String(30), comment="Benzin, Dizel, Elektrik vb. (Ileride tabloya donusebilir)")
    kilometre = Column(Integer, comment="Aracin anlik kilometresi")
    
    kayit_tarihi = Column(DateTime, default=datetime.datetime.utcnow, comment="Aracin sisteme eklenme zamani")
    
    sahip = relationship("Kullanici", back_populates="araclar")
    servis_talepleri = relationship("ServisTalebi", back_populates="arac")
    # servis_talepleri = relationship("ServisTalebi")
    marka = relationship("Marka")
    model = relationship("Model")
    
    kayit_durumu = Column(String(1), default="A", comment="A: Aktif Kayit, X: Silinmis (Soft Delete)")
    silinme_tarihi = Column(DateTime, nullable=True, comment="Kaydin X durumuna cekildigi tarih")

    
class ServisTalebi(Base):
    __tablename__ = "servis_talepleri"
    __table_args__ = {'comment': 'Musterilerin olusturdugu, fiyat ve onay mekanizmali operasyon tablosu.'}

    id = Column(Integer, primary_key=True, index=True, comment="Talep tekil kimligi (PK)")
    
    # Yabancı Anahtarlar (Foreign Keys)
    kullanici_id = Column(Integer, ForeignKey("kullanicilar.id"), nullable=False, comment="Talebi acan musteri ID'si")
    arac_id = Column(Integer, ForeignKey("araclar.id"), nullable=False, comment="Talebe konu olan arac ID'si")
    hizmet_id = Column(Integer, ForeignKey("hizmetler.id"), nullable=False, comment="Secilen hizmetin ID'si")
    
    # Form Verileri
    talep_tarihi = Column(Date, nullable=False, comment="Musterinin istedigi randevu tarihi")
    adres = Column(Text, nullable=False, comment="Hizmetin verilecegi adres")
    notlar = Column(Text, nullable=True, comment="Ustaya iletilen ek notlar")
    
    # Onay ve Revize Mekanizması
    durum = Column(String(50), default="Bekliyor", comment="Talebin durumu (Bekliyor, Onaylandi, Revize Bekliyor, Iptal)")
    onerilen_tarih = Column(DateTime, nullable=True, comment="Usta tarafindan onerilen yeni tarih")
    
    # Tarih Damgaları
    insert_tarihi = Column(DateTime, server_default=func.now(), comment="Talebin olusturulma zamani")
    guncelleme_tarihi = Column(DateTime, server_default=func.now(), onupdate=func.now(), comment="Talebin son islem gorme zamani")
        
    kayit_durumu = Column(String(1), default="A", comment="A: Aktif Kayit, X: Silinmis (Soft Delete)")
    silinme_tarihi = Column(DateTime, nullable=True, comment="Kaydin X durumuna cekildigi tarih")
    tahmini_tutar = Column(Float, default=0.0)  # YENİ EKLENEN KOLON
    duzeltme_istendi_mi = Column(Boolean, default=False)
    duzeltme_notu = Column(String(500), nullable=True)
    
    tamamlanma_tarihi = Column(DateTime, nullable=True)    
    iptal_eden_id = Column(Integer, ForeignKey("kullanicilar.id"), nullable=True, comment="Talebi iptal eden kişinin ID'si")
        
    # ORM İlişkileri (Eski kodundan miras aldığımız, tabloları birbirine bağlayan kısım)
    # musteri = relationship("Kullanici", back_populates="servis_talepleri")
    # arac = relationship("Arac", back_populates="servis_talepleri")
    
    # ORM İlişkileri (Zorlama bağlantılar kaldırıldı, tek yönlü güvenli bağlantı yapıldı)
    # musteri = relationship("Kullanici")
    # arac = relationship("Arac")
    hizmet = relationship("Hizmet")
    musteri = relationship("Kullanici", foreign_keys="[ServisTalebi.kullanici_id]", back_populates="servis_talepleri")
    arac = relationship("Arac", back_populates="servis_talepleri")

class Hizmet(Base):
    __tablename__ = "hizmetler"
    __table_args__ = {'comment': 'Musterilere sunulan bakim ve onarim hizmetlerinin fiyat listesi.'}

    id = Column(Integer, primary_key=True, index=True, comment="Hizmet tekil kimligi (PK)")
    ad = Column(String(100), nullable=False, comment="Hizmetin vitrin adi")
    aciklama = Column(Text, nullable=True, comment="Hizmet icerigi ve detaylari")
    varsayilan_fiyat = Column(Numeric(10, 2), nullable=False, comment="Mevcut guncel satis fiyati")
    onceki_fiyat = Column(Numeric(10, 2), nullable=True, comment="Bir onceki satis fiyati")
    
    # func.now() ile MySQL'in CURRENT_TIMESTAMP özelliğini yakalıyoruz
    insert_tarihi = Column(DateTime, server_default=func.now(), comment="Hizmetin sisteme eklenme tarihi")
    guncelleme_tarihi = Column(DateTime, server_default=func.now(), onupdate=func.now(), comment="Fiyat veya detayin son degisim tarihi")
    
    fiyat_gecmisi = relationship("HizmetFiyatGecmisi", back_populates="hizmet")

class HizmetFiyatGecmisi(Base):
    __tablename__ = "hizmet_fiyat_gecmisi"
    __table_args__ = {'comment': 'Hizmet fiyatlarindaki degisimleri tarihsel olarak tutan log/arsiv tablosu.'}

    id = Column(Integer, primary_key=True, index=True, comment="Arsiv kaydi tekil kimligi (PK)")
    hizmet_id = Column(Integer, ForeignKey("hizmetler.id"), nullable=False, comment="Fiyati degisen hizmetin ID'si (FK)")
    eski_fiyat = Column(Numeric(10, 2), nullable=False, comment="Degisim oncesi fiyat")
    yeni_fiyat = Column(Numeric(10, 2), nullable=False, comment="Degisim sonrasi yeni fiyat")
    
    insert_tarihi = Column(DateTime, server_default=func.now(), comment="Degisimin yapildigi tarih ve saat")

    hizmet = relationship("Hizmet", back_populates="fiyat_gecmisi")

class SistemLog(Base):
    __tablename__ = "sistem_loglari"
    __table_args__ = {'comment': 'Uygulama genelindeki kritik hatalari (500) ve traceback ciktilarini tutan log tablosu.'}
    
    id = Column(Integer, primary_key=True, index=True, comment="Log tekil kimligi (PK)")
    kullanici_id = Column(Integer, nullable=True, comment="Hata aninda islem yapan kullanici (Varsa)")
    seviye = Column(String(20), default="ERROR", comment="Log seviyesi (ERROR, WARNING, INFO)")
    islem = Column(String(100), nullable=False, comment="Hatanin alindigi Endpoint/URL")
    detay = Column(Text, nullable=False, comment="Hatanin teknik detayi ve Traceback metni")
    insert_tarihi = Column(DateTime, server_default=func.now(), comment="Hatanin olustugu tarih ve saat")
    
# ==============================================================================
# TABLO: SISTEM_BILDIRIMLERI
# AÇIKLAMA: Kullanıcılara gönderilen uygulama içi ve push bildirimlerin geçmişini tutar.
# ==============================================================================
class SistemBildirimleri(Base):
    """
    Kullanıcıya özel bildirimlerin saklandığı tablo.
    """
    __tablename__ = "sistem_bildirimleri"

    # [PK] Bildirim ID
    id = Column(Integer, primary_key=True, index=True, comment="Bildirimin benzersiz numarası")
    
    # [FK] Bildirimin sahibi olan kullanıcı
    kullanici_id = Column(Integer, ForeignKey("kullanicilar.id"), nullable=False, comment="Bildirimin gönderildiği kullanıcının ID'si")
    
    # Bildirim Başlığı (Kısa)
    baslik = Column(String(100), nullable=False, comment="Bildirimin kısa başlığı (Örn: Talep Güncellendi)")
    
    # Bildirim Detay Mesajı
    mesaj = Column(String(500), nullable=False, comment="Bildirimin detaylı mesaj içeriği")
    
    # Okundu Bilgisi (Android/iOS tarafında tetiklenir)
    okundu_mu = Column(Boolean, default=False, comment="Kullanıcının bildirimi okuyup okumadığı bilgisi")
    
    # Oluşturulma Zamanı (Yılan gibi FastAPI func.now() ile otomatik atar)
    olusturulma_tarihi = Column(DateTime(timezone=True), server_default=func.now(), comment="Bildirimin oluşturulduğu tarih ve saat")
    
    
# ==============================================================================
# TABLO: SERVIS_TALEBI_FOTOGRAFLARI
# ==============================================================================
class ServisTalebiFotograf(Base):
    __tablename__ = "servis_talebi_fotograflar"
    __table_args__ = {'comment': 'Servis taleplerine eklenen hasar fotograflari.'}

    id = Column(Integer, primary_key=True, index=True)
    talep_id = Column(Integer, ForeignKey("servis_talepleri.id"), nullable=False)
    dosya_yolu = Column(String(255), nullable=False)
    olusturulma_tarihi = Column(DateTime, server_default=func.now())

    # ServisTalebi modelinle ilişkilendirme (İleride çekmek istersen diye)
    talep = relationship("ServisTalebi", backref="fotograflar")