from pydantic import BaseModel, EmailStr
from typing import Optional, List
from datetime import date, datetime

# --- REFERANS ŞEMALARI ---
class AracModelBase(BaseModel):
    ad: str

class AracModel(AracModelBase):
    id: int
    marka_id: int
    class Config:
        from_attributes = True # SQLAlchemy modellerini Pydantic şemalarına dönüştürmek için

class MarkaBase(BaseModel):
    ad: str

class Marka(MarkaBase):
    id: int
    modeller: List[AracModel] = []
    class Config:
        from_attributes = True

# --- ARAÇ ŞEMALARI ---
class AracBase(BaseModel):
    marka_id: Optional[int] = None
    model_id: Optional[int] = None
    ozel_marka: Optional[str] = None
    ozel_model: Optional[str] = None
    yil: int
    yakit_tipi: str
    kilometre: int

class AracCreate(AracBase):
    sahip_id: int

class Arac(AracBase):
    id: int
    sahip_id: int
    kayit_tarihi: datetime
    class Config:
        from_attributes = True


# --- SERVİS TALEBİ ŞEMALARI ---
class ServisTalebiBase(BaseModel):
    kullanici_id: int
    arac_id: int
    hizmet_id: int
    talep_tarihi: date
    adres: str
    notlar: Optional[str] = None

class ServisTalebiCreate(ServisTalebiBase):
    pass # Base içindeki her şeyi miras alır, yeni bir şeye gerek yok.

class ServisTalebi(ServisTalebiBase):
    id: int
    durum: str
    onerilen_tarih: Optional[datetime] = None
    insert_tarihi: datetime
    guncelleme_tarihi: datetime    
    # --- YENİ REVİZE: MAUI tarafına tarihlerin gitmesi için bu alanları ekliyoruz ---
    tamamlanma_tarihi: Optional[datetime] = None
    silinme_tarihi: Optional[datetime] = None
    # ---------------------------------------------------------------------------------

    class Config:
        from_attributes = True


# --- KULLANICI ŞEMALARI ---
class KullaniciBase(BaseModel):
    ad_soyad: str
    eposta: EmailStr
    telefon: str
    adres: Optional[str] = None  # YENİ EKLENDİ
    rol: Optional[str] = "Musteri" # YENİ EKLENEN SATIR

class KullaniciCreate(KullaniciBase):
    sifre: str 

class Kullanici(KullaniciBase):
    id: int
    aktif_mi: bool
    kayit_tarihi: datetime
    araclar: List[Arac] = []
    servis_talepleri: List[ServisTalebi] = []
    class Config:
        from_attributes = True
        
class KullaniciGiris(BaseModel):
    eposta: EmailStr
    sifre: str
    
class TokenKayitIstegi(BaseModel):
    kullanici_id: int
    fcm_token: str    
    
# Güncelleme işleminde şifre veya e-posta değişmeyecek, sadece bu alanlar değişecek
class KullaniciUpdate(BaseModel):
    ad_soyad: str
    telefon: str
    adres: Optional[str] = None

#Servis talepleri
from datetime import date

class Hizmet(BaseModel):
    id: int
    ad: str
    aciklama: Optional[str] = None
    varsayilan_fiyat: float

    class Config:
        from_attributes = True

class ServisTalebiCreate(BaseModel):
    kullanici_id: int
    arac_id: int
    hizmet_id: int
    talep_tarihi: date
    adres: str
    notlar: Optional[str] = None
    
class TalepGuncelleKullanici(BaseModel):
    hizmet_id: Optional[int] = None
    arac_id: Optional[int] = None
    talep_tarihi: Optional[str] = None
    adres: Optional[str] = None
    notlar: Optional[str] = None
    duzeltme_istendi_mi: Optional[bool] = False
    duzeltme_notu: Optional[str] = None
    
class BildirimResponse(BaseModel):
    id: int
    baslik: str
    mesaj: str
    okundu_mu: bool
    olusturulma_tarihi: datetime

    # PYDANTIC V2 STANDARDI (Warning çözümü): 
    class Config:
        from_attributes = True
        
class TalepAdminGuncelle(BaseModel):
    yeni_durum: str
    tahmini_tutar: float
    islem_yapan_id: Optional[int] = None # Adminin kim olduğunu anlamak için