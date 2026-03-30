from fastapi import FastAPI, Depends, HTTPException
from sqlalchemy.orm import Session
import models, schemas
from database import engine, get_db
import helpers  # Yeni helper dosyamızı içeri aldık
from typing import List  # Eğer en üstte yoksa bunu eklemeyi unutma

models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="Kapıdan Bakım API", version="1.0.0")

@app.get("/")
def read_root():
    return {"mesaj": "Kapıdan Bakım API Sistemine Hoş Geldiniz!", "durum": "Aktif"}

# --- VERİTABANI BAŞLANGIÇ VERİLERİ (SEED) ---
@app.post("/kurulum/referans-verilerini-yukle")
def referans_verilerini_yukle(db: Session = Depends(get_db)):
    # Sisteme ilk kurulduğunda eklenecek varsayılan veriler
    ornek_veri = {
        "BMW": ["1.16i", "3.20i", "5.20d", "X5"],
        "Fiat": ["Doblo", "Egea", "Fiorino", "Punto"],
        "Ford": ["Fiesta", "Focus", "Kuga", "Puma"],
        "Mercedes": ["A180", "C180", "E250", "GLA"],
        "Renault": ["Captur", "Clio", "Megane", "Taliant"],
        "Volkswagen": ["Golf", "Passat", "Polo", "Tiguan"],
        "Toyota": ["Corolla", "Yaris", "Auris", "C-HR"]
    }
    
    # Çoklu yüklemeyi önlemek için kontrol
    mevcut_marka_sayisi = db.query(models.Marka).count()
    if mevcut_marka_sayisi > 0:
        return {"mesaj": "Veritabaninda zaten referans verileri mevcut, islem iptal edildi."}

    # Verileri tablolara yazma döngüsü
    for marka_adi, modeller in ornek_veri.items():
        yeni_marka = models.Marka(ad=marka_adi)
        db.add(yeni_marka)
        db.commit()
        db.refresh(yeni_marka) # Eklenen markanın oluşan ID'sini almak için
        
        for model_adi in modeller:
            yeni_model = models.Model(ad=model_adi, marka_id=yeni_marka.id)
            db.add(yeni_model)
        db.commit() # Tüm modelleri tek seferde kaydet
        
    return {"mesaj": "Marka ve modeller veritabanina basariyla eklendi!"}


# --- AÇILIR LİSTELER (DROPDOWN) İÇİN UÇ NOKTALAR ---
@app.get("/referanslar/markalar/", response_model=List[schemas.Marka])
def markalari_getir(db: Session = Depends(get_db)):
    return db.query(models.Marka).all()

@app.get("/referanslar/modeller/{marka_id}")
def modelleri_getir(marka_id: int, db: Session = Depends(get_db)):
    return db.query(models.Model).filter(models.Model.marka_id == marka_id).all()


# --- KULLANICI İŞLEMLERİ ---
@app.post("/kullanicilar/", response_model=schemas.Kullanici)
def kullanici_olustur(kullanici: schemas.KullaniciCreate, db: Session = Depends(get_db)):
    db_kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == kullanici.eposta).first()
    if db_kullanici:
        raise HTTPException(status_code=400, detail="Bu email adresi zaten kayitli.")
    
    yeni_kullanici = models.Kullanici(
        ad_soyad=kullanici.ad_soyad,
        eposta=kullanici.eposta,
        telefon=kullanici.telefon,
        sifre_hash=kullanici.sifre 
    )
    db.add(yeni_kullanici)
    db.commit()
    db.refresh(yeni_kullanici)
    return yeni_kullanici

@app.post("/giris/", response_model=schemas.Kullanici)
def giris_yap(giris_bilgileri: schemas.KullaniciGiris, db: Session = Depends(get_db)):
    # Veritabanında bu e-postaya sahip kullanıcıyı bul
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == giris_bilgileri.eposta).first()
    
    # Kullanıcı yoksa veya şifre eşleşmiyorsa hata fırlat 
    # (Not: Güvenlik aşamasında buradaki şifreyi hash ile karşılaştıracağız, şimdilik düz metin)
    if not kullanici or kullanici.sifre_hash != giris_bilgileri.sifre:
        raise HTTPException(status_code=401, detail="E-posta veya şifre hatali")
    
    # Hesap pasife alınmış mı kontrolü
    if not kullanici.aktif_mi:
        raise HTTPException(status_code=403, detail="Hesabiniz askiya alinmistir")
        
    return kullanici

@app.put("/kullanicilar/{kullanici_id}", response_model=schemas.Kullanici)
def kullanici_guncelle(kullanici_id: int, guncel_veri: schemas.KullaniciUpdate, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanici bulunamadi")
    
    kullanici.ad_soyad = guncel_veri.ad_soyad
    kullanici.telefon = guncel_veri.telefon
    kullanici.adres = guncel_veri.adres
    
    db.commit()
    db.refresh(kullanici)
    return kullanici

# --- ARAÇ İŞLEMLERİ ---
@app.post("/araclar/", response_model=schemas.Arac)
def arac_ekle(arac: schemas.AracCreate, db: Session = Depends(get_db)):
    yeni_arac = models.Arac(**arac.model_dump())
    db.add(yeni_arac)
    db.commit()
    db.refresh(yeni_arac)
    return yeni_arac

# --- SERVİS TALEBİ İŞLEMLERİ ---
@app.post("/servis-talepleri/", response_model=schemas.ServisTalebi)
def servis_talebi_olustur(talep: schemas.ServisTalebiCreate, db: Session = Depends(get_db)):
    # 1. Fiyat hesaplayabilmek için önce talebin yapıldığı aracı ve markasını DB'den bulmamız lazım
    arac = db.query(models.Arac).filter(models.Arac.id == talep.arac_id).first()
    
    if not arac:
        raise HTTPException(status_code=404, detail="Talep oluşturulacak araç bulunamadı.")

    # Marka ID'si varsa ismini bul, yoksa (özel markaysa) özel markayı al
    marka_adi = "Bilinmiyor"
    if arac.marka_id:
        marka_obj = db.query(models.Marka).filter(models.Marka.id == arac.marka_id).first()
        if marka_obj:
            marka_adi = marka_obj.ad
    elif arac.ozel_marka:
        marka_adi = arac.ozel_marka

    # 2. Helper dosyamızdaki uzman motoruna bilgileri gönderip fiyatı alıyoruz
    hesaplanan_fiyat = helpers.FiyatlandirmaMotoru.tahmini_fiyat_hesapla(
        marka_adi=marka_adi
        # vale_istendi_mi=talep.vale_istendi_mi
    )

    # 3. Talebi oluştur ve hesaplanan fiyatı içine yaz
    yeni_talep = models.ServisTalebi(
        **talep.model_dump(),
        tahmini_fiyat=hesaplanan_fiyat  # Helper'dan gelen fiyatı DB'ye kaydediyoruz
    )
    
    db.add(yeni_talep)
    db.commit()
    db.refresh(yeni_talep)
    
    return yeni_talep

@app.get("/kullanicilar/{kullanici_id}", response_model=schemas.Kullanici)
def kullanici_getir(kullanici_id: int, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    if kullanici is None:
        raise HTTPException(status_code=404, detail="Kullanici bulunamadi")
    return kullanici

# --- HİZMETLER VE SERVİS TALEPLERİ ---

@app.get("/referanslar/hizmetler/", response_model=List[schemas.Hizmet])
def hizmetleri_getir(db: Session = Depends(get_db)):
    hizmetler = db.query(models.Hizmet).all()
    return hizmetler

from fastapi import HTTPException

@app.post("/servis-talepleri/")
def servis_talebi_olustur(talep: schemas.ServisTalebiCreate, db: Session = Depends(get_db)):
    try:
        # Pydantic modelini veritabanı nesnesine çeviriyoruz
        yeni_talep = models.ServisTalebi(**talep.dict())
        db.add(yeni_talep)
        db.commit()
        db.refresh(yeni_talep)
        return yeni_talep
    except Exception as e:
        # İşlem başarısız olursa veritabanını geri al ve C# tarafına net hatayı fırlat
        db.rollback()
        raise HTTPException(status_code=500, detail=f"Veritabanı Kayıt Hatası: {str(e)}")

@app.put("/hizmetler/{hizmet_id}/fiyat")
def hizmet_fiyat_guncelle(hizmet_id: int, yeni_fiyat: float, db: Session = Depends(get_db)):
    hizmet = db.query(models.Hizmet).filter(models.Hizmet.id == hizmet_id).first()
    
    if not hizmet:
        raise HTTPException(status_code=404, detail="Hizmet bulunamadi")
    
    # 1. Eski fiyatı kenara not al
    eski_fiyat = hizmet.varsayilan_fiyat
    
    # 2. Ana tablodaki değerleri kaydır ve güncelle
    hizmet.onceki_fiyat = eski_fiyat
    hizmet.varsayilan_fiyat = yeni_fiyat
    
    # 3. Arşiv (Geçmiş) tablosuna yepyeni bir kayıt at
    gecmis_kaydi = models.HizmetFiyatGecmisi(
        hizmet_id=hizmet.id,
        eski_fiyat=eski_fiyat,
        yeni_fiyat=yeni_fiyat
    )
    db.add(gecmis_kaydi)
    db.commit()
    db.refresh(hizmet)
    
    return {"mesaj": "Fiyat basariyla guncellendi ve arsive eklendi", "guncel_fiyat": yeni_fiyat}