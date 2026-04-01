from fastapi import FastAPI, Depends, HTTPException, APIRouter
from sqlalchemy.orm import Session
import models, schemas
from database import engine, get_db
from typing import List
import logging
from logging.handlers import RotatingFileHandler
from fastapi import Request
from fastapi.responses import JSONResponse
from starlette.middleware.base import BaseHTTPMiddleware
import traceback
from datetime import datetime
import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
import random
import string
from sqlalchemy import Column, Integer, String, Boolean, ForeignKey, DateTime, Float, case
import asyncio
import requests
from database import SessionLocal # Arka plan görevleri için bağımsız bir DB oturumu lazım.
from datetime import date, datetime, time
from typing import Optional
from fastapi import Query
import math
import firebase_admin
from firebase_admin import credentials, messaging

models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="Kapıdan Bakım API", version="1.0.0")

# Firebase Başlatma (Eğer yoksa main.py'nin üst kısımlarına ekle)
if not firebase_admin._apps:
    # Firebase Console -> Proje Ayarları -> Hizmet Hesapları -> Yeni Özel Anahtar Oluştur diyerek indirdiğin JSON dosyasının adını buraya yaz:
    cred = credentials.Certificate("firebase-adminsdk.json") 
    firebase_admin.initialize_app(cred)

# --- DOSYA TABANLI LOGLAMA AYARLARI ---
# Hataları "app_error.log" dosyasına yazar. Dosya 5MB olunca yeni dosyaya geçer (maksimum 5 yedek tutar)..
log_formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(message)s')
log_handler = RotatingFileHandler('app_error.log', maxBytes=5000000, backupCount=5)
log_handler.setFormatter(log_formatter)
logger = logging.getLogger("OtoServisLogger")
logger.setLevel(logging.ERROR)
logger.addHandler(log_handler)

# --- MIDDLEWARE (TÜM HATALARI YAKALAYAN KALKAN) ---
@app.middleware("http")
async def catch_exceptions_middleware(request: Request, call_next):
    try:
        # İstek normal şekilde çalışırsa devam et
        return await call_next(request)
    except Exception as e:
        # 1. Hatanın tam dökümünü (Traceback) sunucudaki .log dosyasına yaz
        hata_detayi = traceback.format_exc()
        logger.error(f"URL: {request.url.path} | Hata: {str(e)}\n{hata_detayi}")
        
        # 2. Hatanın özetini veritabanındaki sistem_loglari tablosuna yaz
        try:
            db = next(get_db())
            yeni_log = models.SistemLog(
                seviye="ERROR",
                islem=f"{request.method} {request.url.path}",
                detay=str(e) # Veritabanını şişirmemek için sadece kısa hatayı atıyoruz
            )
            db.add(yeni_log)
            db.commit()
        except:
            pass # Eğer veritabanı da çökmüşse, döngüye girmemesi için pass diyoruz
            
        # 3. Kullanıcıya temiz, kibar ve kapalı bir 500 hatası dön
        return JSONResponse(
            status_code=500,
            content={"detail": "Sunucu tarafında beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin."}
        )

@app.get("/")
def read_root():
    return {"mesaj": "Kapıdan Bakım API Sistemine Hoş Geldiniz!", "durum": "Aktif"}


# ------------------------------------------------------------------ #
# ------------------------------------------------------------------ #
# ------------------------------------------------------------------ #

# --- VERİTABANI BAŞLANGIÇ VERİLERİ (SEED) ---
r"""
@app.post("/kurulum/referans-verilerini-yukle")
def referans_verilerini_yukle(db: Session = Depends(get_db)):
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
"""

# --- DİNAMİK ARAÇ GÜNCELLEYİCİ (HER ÇALIŞTIĞINDA SADECE YENİLERİ EKLER) ---
# --- TÜRKİYE'YE ÖZEL GARANTİLİ ARAÇ LİSTESİ ---
# GitHub'da eksik olan popüler araçları buraya manuel ekliyoruz.
TURKIYE_OZEL_ARACLAR = {
    "Peugeot": ["208", "308", "408", "508", "2008", "3008", "5008", "Rifter", "Partner"],
    "Togg": ["T10X", "T10F"],
    "Chery": ["Tiggo 7 Pro", "Tiggo 8 Pro", "Omoda 5"],
    "Fiat": ["Egea", "Egea Cross", "Fiorino", "Doblo", "500", "500X"],
    "Renault": ["Megane", "Clio", "Taliant", "Captur", "Austral", "Koleos", "Kangoo"],
    "Dacia": ["Duster", "Sandero", "Sandero Stepway", "Jogger", "Spring"],
    "Hyundai": ["i20", "i10", "Tucson", "Bayon", "Elantra", "Kona"],
    "Toyota": ["Corolla", "Corolla Cross", "C-HR", "Yaris", "Yaris Cross", "Hilux"],
    "Volkswagen": ["Golf", "Polo", "Passat", "T-Roc", "Tiguan", "Taigo", "Caddy", "Amarok"],
    "Skoda": ["Octavia", "Superb", "Scala", "Kamiq", "Karoq", "Kodiaq"],
    "Honda": ["Civic", "City", "HR-V", "ZR-V", "CR-V"],
    "Ford": ["Focus", "Puma", "Kuga", "Tourneo Courier", "Transit"],
    "Opel": ["Corsa", "Astra", "Crossland", "Mokka", "Grandland"],
    "Citroen": ["C3", "C3 Aircross", "C4", "C4 X", "C5 Aircross", "Berlingo"]
}

# --- DİNAMİK ARAÇ GÜNCELLEYİCİ (GITHUB + TÜRKİYE PAKETİ) ---
def arac_verilerini_guncelle(db: Session):
    print("⏳ Araç verileri kontrol ediliyor (GitHub + Türkiye Paketi)...")
    try:
        url = "https://raw.githubusercontent.com/matthlavacka/car-list/master/car-list.json"
        response = requests.get(url, timeout=15)
        
        car_data = []
        if response.status_code == 200:
            car_data = response.json()
        
        # GitHub verisiyle Türkiye Özel listemizi birleştiriyoruz
        for tr_marka, tr_modeller in TURKIYE_OZEL_ARACLAR.items():
            car_data.append({"brand": tr_marka, "models": tr_modeller})
            
        yeni_marka_sayisi = 0
        yeni_model_sayisi = 0
        
        for item in car_data:
            brand_name = item.get("brand")
            if not brand_name: continue
                
            marka = db.query(models.Marka).filter(models.Marka.ad == brand_name).first()
            if not marka:
                marka = models.Marka(ad=brand_name)
                db.add(marka)
                db.flush() 
                yeni_marka_sayisi += 1
            
            models_list = item.get("models", [])
            for model_name in models_list:
                model_var_mi = db.query(models.Model).filter(models.Model.ad == model_name, models.Model.marka_id == marka.id).first()
                if not model_var_mi:
                    db.add(models.Model(ad=model_name, marka_id=marka.id))
                    yeni_model_sayisi += 1
                    
        db.commit()
        if yeni_marka_sayisi > 0 or yeni_model_sayisi > 0:
            print(f"✅ Araç Güncellemesi Tamamlandı: {yeni_marka_sayisi} Yeni Marka, {yeni_model_sayisi} Yeni Model eklendi.")
        else:
            print("✅ Araç listesi zaten güncel.")
    except Exception as e:
        print(f"❌ Araç verileri güncellenirken hata: {e}")

# --- 60 KALEMLİK DEV HİZMET LİSTESİ ---
def hizmet_verilerini_tohumla(db: Session):
    if db.query(models.Hizmet).count() == 0:
        print("⏳ 60 Kalemlik Kapsamlı Hizmet Listesi veritabanına işleniyor...")
        
        kapsamli_hizmetler = [
            # 1. PERİYODİK BAKIM VE SIVILAR
            {"ad": "Standart Periyodik Bakım", "fiyat": 4500.0, "aciklama": "Motor yağı, yağ filtresi, hava filtresi, polen filtresi değişimi ve sıvı kontrolleri."},
            {"ad": "Kışlık Bakım Paketi", "fiyat": 2500.0, "aciklama": "Antifriz ölçümü ve değişimi, akü testi, kışlık silecek değişimi ve ısıtma sistemi kontrolü."},
            {"ad": "Yazlık Bakım Paketi", "fiyat": 2800.0, "aciklama": "Klima gazı kontrolü, radyatör temizliği, polen filtresi yenilemesi."},
            {"ad": "Ağır Bakım (Triger Seti)", "fiyat": 18000.0, "aciklama": "Triger kayışı/zinciri, devirdaim pompası, V kayışı ve gergi bilyası değişimi."},
            {"ad": "Motor Yağı ve Filtre Değişimi", "fiyat": 2000.0, "aciklama": "Orijinal spesifikasyonlara uygun motor yağı ve yağ filtresi değişimi."},
            {"ad": "Antifriz Değişimi", "fiyat": 1200.0, "aciklama": "Soğutma sistemindeki eski suyun boşaltılıp yeni organik antifriz konulması."},
            {"ad": "Fren Hidroliği Değişimi", "fiyat": 1500.0, "aciklama": "Fren hidrolik sıvısının (DOT4/DOT5) makine ile tamamen yenilenmesi."},
            {"ad": "Direksiyon Hidroliği Değişimi", "fiyat": 1000.0, "aciklama": "Hidrolik direksiyon sıvısının yenilenmesi ve havasının alınması."},
            {"ad": "Cam Suyu ve Silecek Lastiği Değişimi", "fiyat": 600.0, "aciklama": "Ön ve arka silecek lastiklerinin değişimi, antifrizli cam suyu eklenmesi."},
            {"ad": "Ekspertiz ve Check-Up", "fiyat": 4000.0, "aciklama": "Kaporta, boya, motor ve mekanik durumun 101 nokta kontrolü ile raporlanması."},

            # 2. FREN SİSTEMİ
            {"ad": "Ön Fren Balata Değişimi", "fiyat": 2500.0, "aciklama": "Ön aks fren balatalarının değişimi, disk yüzey kontrolü ve kaliper yağlaması."},
            {"ad": "Arka Fren Balata Değişimi", "fiyat": 2200.0, "aciklama": "Arka aks fren balatalarının değişimi ve el freni mekanizması ayarı."},
            {"ad": "Ön Fren Disk Değişimi (Çift)", "fiyat": 6500.0, "aciklama": "Çizilmiş, incelmiş veya eğilmiş ön fren disklerinin yenisiyle değiştirilmesi."},
            {"ad": "Arka Fren Disk Değişimi (Çift)", "fiyat": 5500.0, "aciklama": "Arka fren disklerinin yenisiyle değiştirilmesi."},
            {"ad": "Fren Kaliper Revizyonu", "fiyat": 3000.0, "aciklama": "Sıkışan fren kaliperlerinin sökülüp tamir takımı ile yenilenmesi."},
            {"ad": "Fren Merkezi Değişimi", "fiyat": 4500.0, "aciklama": "Fren ana merkezinin arızalanması durumunda yenisiyle değiştirilmesi."},
            {"ad": "Fren Hortumu Değişimi", "fiyat": 1200.0, "aciklama": "Çatlamış veya hasar görmüş esnek fren hortumlarının değişimi."},
            {"ad": "ABS Sensörü Değişimi", "fiyat": 2500.0, "aciklama": "Arıza veren tekerlek hız (ABS) sensörünün tespiti ve değişimi."},
            {"ad": "ABS Beyni Tamiri", "fiyat": 12000.0, "aciklama": "Arızalı ABS modülünün sökülerek elektronik tamirinin yapılması."},
            {"ad": "El Freni Teli Değişimi", "fiyat": 1800.0, "aciklama": "Kopan veya sıkışan el freni telinin yenilenmesi."},

            # 3. MOTOR VE MEKANİK
            {"ad": "Buji Takımı Değişimi (Benzinli)", "fiyat": 2500.0, "aciklama": "4 adet iridyum veya platinium ateşleme bujisinin değişimi."},
            {"ad": "Kızdırma Bujisi Değişimi (Dizel)", "fiyat": 3500.0, "aciklama": "Dizel motor ön ısıtma (kızdırma) bujilerinin takım halinde değişimi."},
            {"ad": "Ateşleme Bobini Değişimi", "fiyat": 4500.0, "aciklama": "Tekleyen veya arıza lambası yakan ateşleme bobininin yenilenmesi."},
            {"ad": "Dizel Partikül Filtresi (DPF) Temizliği", "fiyat": 6000.0, "aciklama": "Tıkalı partikül filtresinin özel makine ile temizlenmesi ve rejenerasyonu."},
            {"ad": "EGR Valfi Temizliği / İptali", "fiyat": 3500.0, "aciklama": "Kurum bağlamış EGR valfinin sökülerek temizlenmesi veya yazılımsal çözümü."},
            {"ad": "Enjektör Bakımı ve Testi", "fiyat": 5000.0, "aciklama": "Dizel/Benzin enjektörlerinin sökülüp cihazda test ve ultrasonik temizliğinin yapılması."},
            {"ad": "Motor Takozu (Kulağı) Değişimi", "fiyat": 3500.0, "aciklama": "Kabin içine titreşim veren kopmuş motor takozunun değişimi."},
            {"ad": "Turbo Revizyonu", "fiyat": 15000.0, "aciklama": "Yağ kaçıran veya ses yapan turbonun sökülüp kartuş değişimi ve revizyonu."},
            {"ad": "Silindir Kapak Contası Değişimi", "fiyat": 25000.0, "aciklama": "Hararet sonucu yanan contanın değişimi, kapak taşlama ve sızdırmazlık testi."},
            {"ad": "Termostat Değişimi", "fiyat": 3000.0, "aciklama": "Açık veya kapalı kalan termostatın yenisiyle değişimi ve su havasının alınması."},
            {"ad": "Radyatör Değişimi", "fiyat": 6000.0, "aciklama": "Su kaçıran veya tıkanan motor soğutma radyatörünün yenilenmesi."},
            {"ad": "Su Pompası (Devirdaim) Değişimi", "fiyat": 4500.0, "aciklama": "Su sızdıran veya ses yapan devirdaim pompasının değişimi."},
            {"ad": "Motor Yağ Kaçağı Onarımı", "fiyat": 5000.0, "aciklama": "Karter, külbütör veya krank keçesindeki yağ kaçaklarının giderilmesi."},
            {"ad": "Katalitik Konvertör Temizliği", "fiyat": 4500.0, "aciklama": "Tıkanmış katalizörün özel kimyasallarla açılarak emisyonun düşürülmesi."},
            {"ad": "Boğaz Kelebeği Temizliği", "fiyat": 1500.0, "aciklama": "Rölanti dalgalanmasına yol açan boğaz kelebeğinin temizlenip adaptasyon yapılması."},

            # 4. ŞANZIMAN VE DEBRİYAJ
            {"ad": "Baskı Balata (Debriyaj) Seti Değişimi", "fiyat": 15000.0, "aciklama": "Manuel araçlarda debriyaj seti, bilya değişimi ve şanzıman yağı ilavesi."},
            {"ad": "Otomatik Şanzıman Yağı Değişimi", "fiyat": 8500.0, "aciklama": "Tam otomatik şanzıman yağının makine ile tam kapasite değişimi ve filtre yenilemesi."},
            {"ad": "DSG / EDC Kavrama Değişimi", "fiyat": 35000.0, "aciklama": "Çift kavramalı şanzımanların kavrama setinin değişimi ve yazılım adaptasyonu."},
            {"ad": "Şanzıman Beyni (Mekatronik) Tamiri", "fiyat": 25000.0, "aciklama": "Vites geçiş sorunu yaratan mekatronik ünitesinin veya tüpünün onarımı."},
            {"ad": "Aks Lalesi ve Körük Değişimi", "fiyat": 2500.0, "aciklama": "Dönüşlerde ses yapan aks kafasının veya yırtık aks körüğünün değişimi."},

            # 5. ALT TAKIM VE SÜSPANSİYON
            {"ad": "Ön Amortisör Değişimi (Çift)", "fiyat": 7000.0, "aciklama": "Patlamış veya işlevini yitirmiş ön amortisörlerin çift olarak değişimi."},
            {"ad": "Arka Amortisör Değişimi (Çift)", "fiyat": 5500.0, "aciklama": "Arka süspansiyon amortisörlerinin değişimi."},
            {"ad": "Z Rot (Askı Rotu) Değişimi", "fiyat": 1500.0, "aciklama": "Çukurlarda lokurtu yapan Z rotların çift taraflı değişimi."},
            {"ad": "Rotil ve Rot Başı Değişimi", "fiyat": 2500.0, "aciklama": "Direksiyon boşluğuna sebep olan rotil ve rot başlarının yenilenmesi."},
            {"ad": "Salıncak (Tabla) Değişimi", "fiyat": 4500.0, "aciklama": "Burçları kopmuş veya eğilmiş salıncakların komple değişimi."},
            {"ad": "Tekerlek Rulmanı (Porya) Değişimi", "fiyat": 3500.0, "aciklama": "Hızlandıkça uğultu yapan tekerlek bilyasının presle sökülüp değişimi."},
            {"ad": "Helezon Yayı Değişimi", "fiyat": 4000.0, "aciklama": "Kırık, paslı veya sarkmış süspansiyon yaylarının çift olarak değişimi."},
            {"ad": "Direksiyon Kutusu Revizyonu", "fiyat": 12000.0, "aciklama": "Yağ kaçıran veya boşluk yapan kremayer direksiyon kutusunun tamiri."},
            {"ad": "Bilgisayarlı Rot Balans Ayarı", "fiyat": 1200.0, "aciklama": "Ön takım onarımı sonrası 4 tekerlek lazerli rot ayarı ve sök-tak balans."},
            {"ad": "Amortisör Takozu Değişimi", "fiyat": 2000.0, "aciklama": "Direksiyonu çevirirken gıcırtı yapan amortisör üst takoz ve bilyasının değişimi."},

            # 6. ELEKTRİK, ELEKTRONİK VE İKLİMLENDİRME
            {"ad": "Akü Değişimi (72 Ah Standart)", "fiyat": 4000.0, "aciklama": "Ömrünü tamamlamış akünün sökülmesi, yeni akü montajı ve şarj dinamosu ölçümü."},
            {"ad": "Şarj Dinamosu (Alternatör) Tamiri", "fiyat": 4500.0, "aciklama": "Akü şarj etmeyen dinamonun kömür, konjektör veya diyot tablosunun değişimi."},
            {"ad": "Marş Dinamosu Tamiri", "fiyat": 4000.0, "aciklama": "Basmayan marş motorunun sökülüp otomatiği ve kömürlerinin yenilenmesi."},
            {"ad": "Klima Gazı Dolumu", "fiyat": 1500.0, "aciklama": "Klima gazının (R134a) makine ile vakumlanıp tam gramajında basılması ve yağlanması."},
            {"ad": "Klima Kompresör Tamiri", "fiyat": 8500.0, "aciklama": "Kavrama yapmayan veya ses yapan klima kompresörünün revizyonu."},
            {"ad": "Kalorifer Peteği Temizliği", "fiyat": 2000.0, "aciklama": "Göğüs sökülmeden özel makine ve ilaçla tıkalı kalorifer peteğinin temizlenmesi."},
            {"ad": "Far Ampulü / Xenon Değişimi", "fiyat": 1200.0, "aciklama": "Patlak kısa/uzun far ampullerinin veya Xenon/LED beyinlerinin değişimi."},
            {"ad": "Far Temizliği ve Parlatma", "fiyat": 1200.0, "aciklama": "Sararmış ve matlaşmış polikarbon far camlarının zımpara ve kloroform buharı ile parlatılması."},
            {"ad": "Sigorta ve Tesisat Onarımı", "fiyat": 2500.0, "aciklama": "Oksitlenmiş tesisat kablolarının, rölelerin ve atan sigortaların tespiti/onarımı."},
            {"ad": "Araç Beyin (ECU) Yazılım Güncellemesi", "fiyat": 3000.0, "aciklama": "Motor veya şanzıman beyninin fabrika çıkışlı en güncel yazılımla güncellenmesi."}
        ]
        
        for h in kapsamli_hizmetler:
            db.add(models.Hizmet(
                ad=h["ad"], 
                aciklama=h["aciklama"],
                varsayilan_fiyat=h["fiyat"]
            ))
        db.commit()
        print(f"✅ Başarılı! Toplam {len(kapsamli_hizmetler)} adet hizmet DB'ye eklendi.")


# --- ARKA PLAN ZAMANLAYICI (24 SAATTE BİR) ---
GUNCELLEME_ARALIGI_SAAT = 24

async def periyodik_arac_guncelleme_gorevi():
    while True:
        db = SessionLocal()
        try:
            arac_verilerini_guncelle(db)
        finally:
            db.close()
            
        print(f"💤 Araç listesi güncelleyici uykuya daldı. {GUNCELLEME_ARALIGI_SAAT} saat sonra yeniden kontrol edecek.")
        # Her 24 saatte bir çalışır. Ayrı bir bot çalıştırmana gerek yok.
        await asyncio.sleep(GUNCELLEME_ARALIGI_SAAT * 3600)


# --- UYGULAMA BAŞLATILDIĞINDA ÇALIŞACAK MOTOR ---
@app.on_event("startup")
async def startup_event():
    db = next(get_db())
    hizmet_verilerini_tohumla(db) # Sadece tablo boşsa 60 kalemi yazar
    
    # 24 Saatlik döngüyü arka planda başlatır, sistemi dondurmaz
    asyncio.create_task(periyodik_arac_guncelleme_gorevi())

# ------------------------------------------------------------------ #
# ------------------------------------------------------------------ #
# ------------------------------------------------------------------ #

# --- AÇILIR LİSTELER (DROPDOWN) İÇİN UÇ NOKTALAR ---
@app.get("/referanslar/markalar/", response_model=List[schemas.Marka])
def markalari_getir(db: Session = Depends(get_db)):
    return db.query(models.Marka).all()

@app.get("/referanslar/modeller/{marka_id}")
def modelleri_getir(marka_id: int, db: Session = Depends(get_db)):
    return db.query(models.Model).filter(models.Model.marka_id == marka_id).all()

@app.get("/referanslar/hizmetler/", response_model=List[schemas.Hizmet])
def hizmetleri_getir(db: Session = Depends(get_db)):
    hizmetler = db.query(models.Hizmet).order_by(models.Hizmet.ad.asc()).all()
    return hizmetler


# --- KULLANICI İŞLEMLERİ ---
@app.post("/kullanicilar/", response_model=schemas.Kullanici)
def kullanici_olustur(kullanici: schemas.KullaniciCreate, db: Session = Depends(get_db)):
    # 1. Önce bu mailde biri var mı diye bakıyoruz
    mevcut_kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == kullanici.eposta).first()
    
    if mevcut_kullanici:
        # 2. Eğer kullanıcı varsa ama hesabı 'X' (Silinmiş) ise, onu DİRİLTİYORUZ!
        if mevcut_kullanici.kayit_durumu == 'X':
            mevcut_kullanici.kayit_durumu = 'A' # Yeniden Aktif
            mevcut_kullanici.sifre_hash = kullanici.sifre # DÜZELTİLDİ: sifre değil sifre_hash
            mevcut_kullanici.ad_soyad = kullanici.ad_soyad
            mevcut_kullanici.telefon = kullanici.telefon
            db.commit()
            db.refresh(mevcut_kullanici)
            log_kaydet(db, "Hesap Diriltme", f"{kullanici.eposta} maili ile eski hesap yeniden aktif edildi.", "INFO", mevcut_kullanici.id)
            return mevcut_kullanici
        else:
            # 3. Hesap varsa ve zaten Aktifse ('A'), hata ver!
            raise HTTPException(status_code=400, detail="Bu e-posta adresi sistemde zaten kayıtlı!")

    # 4. Hiç kayıt yoksa sıfırdan oluştur (DÜZELTİLDİ: Manuel ve doğru eşleştirme)
    yeni_kullanici = models.Kullanici(
        ad_soyad=kullanici.ad_soyad,
        eposta=kullanici.eposta,
        telefon=kullanici.telefon,
        sifre_hash=kullanici.sifre # Veritabanındaki adı sifre_hash
    )
    yeni_kullanici.kayit_durumu = 'A' # Yeni kayıt aktif başlar
    db.add(yeni_kullanici)
    db.commit()
    db.refresh(yeni_kullanici)
    
    log_kaydet(db, "Yeni Kayıt", f"{kullanici.eposta} sisteme yeni kayıt oldu.", "INFO", yeni_kullanici.id)
    return yeni_kullanici

# --- HESAP SİLME (SOFT DELETE) UÇ NOKTASI ---
@app.delete("/kullanicilar/{kullanici_id}")
def kullanici_sil(kullanici_id: int, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı")
        
    kullanici.kayit_durumu = 'X' # Soft delete!
    
    log_kaydet(db, "Hesap Silme", f"Kullanıcı ID: {kullanici_id} hesabını sildi ('X' yapıldı).", "WARNING", kullanici_id)
    db.commit()
    return {"mesaj": "Hesabınız başarıyla silindi."}

@app.get("/kullanicilar/{kullanici_id}", response_model=schemas.Kullanici)
def kullanici_getir(kullanici_id: int, db: Session = Depends(get_db)):
    # 1. Sadece aktif kullanıcıyı getir
    kullanici = db.query(models.Kullanici).filter(
        models.Kullanici.id == kullanici_id,
        models.Kullanici.kayit_durumu == 'A'
    ).first()
    
    if kullanici is None:
        raise HTTPException(status_code=404, detail="Kullanici bulunamadi")
        
    # 2. HAYAT KURTARAN FİLTRE: Kullanıcının silinmiş (X) kayıtlarını temizle
    kullanici.araclar = [arac for arac in kullanici.araclar if arac.kayit_durumu == 'A']
    kullanici.servis_talepleri = [talep for talep in kullanici.servis_talepleri if getattr(talep, 'kayit_durumu', 'A') == 'A']
    
    return kullanici

@app.post("/giris/", response_model=schemas.Kullanici)
def giris_yap(giris_bilgileri: schemas.KullaniciGiris, db: Session = Depends(get_db)):
    # 1. Sadece hesabı silinmemiş (A) olan kullanıcıyı bul
    kullanici = db.query(models.Kullanici).filter(
        models.Kullanici.eposta == giris_bilgileri.eposta,
        models.Kullanici.kayit_durumu == 'A'
    ).first()
    # Kullanıcı yoksa veya şifre eşleşmiyorsa hata fırlat 
    # (Not: Güvenlik aşamasında buradaki şifreyi hash ile karşılaştıracağız, şimdilik düz metin)			    
    if not kullanici or kullanici.sifre_hash != giris_bilgileri.sifre:
        raise HTTPException(status_code=401, detail="E-posta veya şifre hatali")
    
    # Hesap pasife alınmış mı kontrolü
    if not kullanici.aktif_mi:
        raise HTTPException(status_code=403, detail="Hesabiniz askiya alinmistir")
        
    # 2. HAYAT KURTARAN FİLTRE: C# tarafına veriyi göndermeden önce silinmiş (X) araçları ve talepleri listeden atıyoruz
    kullanici.araclar = [arac for arac in kullanici.araclar if arac.kayit_durumu == 'A']
    kullanici.servis_talepleri = [talep for talep in kullanici.servis_talepleri if getattr(talep, 'kayit_durumu', 'A') == 'A']
        
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

@app.get("/araclar/kullanici/{kullanici_id}", response_model=List[schemas.Arac])
def kullanici_araclarini_getir(kullanici_id: int, db: Session = Depends(get_db)):
    # Sadece o kullanıcıya ait olan ve silinmemiş (A) araçları getirir
    araclar = db.query(models.Arac).filter(
        models.Arac.sahip_id == kullanici_id,
        models.Arac.kayit_durumu == 'A'
    ).all()
    return araclar

# --- MADDE 28: ARAÇTA AKTİF TALEP VAR MI KONTROLÜ ---
@app.get("/araclar/{arac_id}/aktif-talep-kontrol")
def aktif_talep_kontrol(arac_id: int, db: Session = Depends(get_db)):
    aktif_talep = db.query(models.ServisTalebi).filter(
        models.ServisTalebi.arac_id == arac_id,
        models.ServisTalebi.durum.in_(['Bekliyor', 'Onaylandı', 'İşlemde']),
        models.ServisTalebi.kayit_durumu == 'A'
    ).first()
    
    return {"aktif_talep_var": aktif_talep is not None}

# --- MADDE 28: GÜVENLİ ARAÇ GÜNCELLEME ---
# 28-Aracı Düzenle ekranında sadece kilometre bilgisi güncellenebiliyor, burada marka model değişikliği de yapılmalı ama bu araca kayıtlı bekliyor 
# statüsü dışında bir servis talebi varsa değiştiremesin ve uyarı versin servis talebi mevcut sadece yıl ve km. bilgisi değiştirilebilir 
# diye ve öyle bir statüde olan araç gerçekten sadece yıl ve km değiştirebilsin. marka modeli değiştiremesin. 
@app.put("/araclar/{arac_id}")
def arac_guncelle(arac_id: int, arac_data: schemas.AracCreate, db: Session = Depends(get_db)):
    mevcut_arac = db.query(models.Arac).filter(models.Arac.id == arac_id, models.Arac.kayit_durumu == 'A').first()
    if not mevcut_arac:
        raise HTTPException(status_code=404, detail="Araç bulunamadı.")

    # Araç üzerinde aktif bir işlem var mı bakıyoruz
    aktif_talep = db.query(models.ServisTalebi).filter(
        models.ServisTalebi.arac_id == arac_id,
        models.ServisTalebi.durum.in_(['Bekliyor', 'Onaylandı', 'İşlemde']),
        models.ServisTalebi.kayit_durumu == 'A'
    ).first()

    if aktif_talep:
        # AKTİF TALEP VARSA: Sadece KM ve Üretim Yılı güncellenebilir! Marka/Model değiştirilemez.
        mevcut_arac.yil = arac_data.yil
        mevcut_arac.kilometre = arac_data.kilometre
    else:
        # AKTİF TALEP YOKSA: Her şey serbestçe güncellenebilir.
        mevcut_arac.marka_id = arac_data.marka_id
        mevcut_arac.model_id = arac_data.model_id
        mevcut_arac.ozel_marka = arac_data.ozel_marka
        mevcut_arac.ozel_model = arac_data.ozel_model
        mevcut_arac.yil = arac_data.yil
        mevcut_arac.kilometre = arac_data.kilometre

    db.commit()
    db.refresh(mevcut_arac)
    
    log_kaydet(db, "Araç Güncelleme", f"Araç ID: {arac_id} güncellendi.", "INFO", mevcut_arac.sahip_id)
    return mevcut_arac


# --- SERVİS TALEBİ İŞLEMLERİ ---
# @app.post("/servis-talepleri/")
# def servis_talebi_olustur(talep: schemas.ServisTalebiCreate, db: Session = Depends(get_db)):
#     try:
#         # Pydantic modelini veritabanı nesnesine çeviriyoruz (Temiz, yeni yöntem)
#         yeni_talep = models.ServisTalebi(**talep.model_dump())
#         db.add(yeni_talep)
#         db.commit()
#         db.refresh(yeni_talep)
#         return yeni_talep
    
 # @app.post("/servis-talepleri/", response_model=schemas.ServisTalebi)
# def servis_talebi_olustur(istek: schemas.ServisTalebiCreate, db: Session = Depends(get_db)):
#     yeni_talep = models.ServisTalebi(**istek.model_dump())
#     db.add(yeni_talep)
#     db.commit()
#     db.refresh(yeni_talep)
#     return yeni_talep

# --- 2. YENİ SERVİS TALEBİ OLUŞTURMA METODU ---
@app.post("/servis-talepleri/", response_model=schemas.ServisTalebi)
def servis_talebi_olustur(istek: schemas.ServisTalebiCreate, db: Session = Depends(get_db)):
    try:
        # 1. Yeni servis talebini oluşturuyoruz						
        yeni_talep = models.ServisTalebi(**istek.model_dump())
        db.add(yeni_talep)
        # YENİ REVİZE: commit() yerine flush() kullanıyoruz. 
        # Böylece talep beklemeye alınıyor, hata olursa geri alınabilecek (rollback).													 
        db.flush() 

        # 2. Admin'e Yeni Talep Bildirimi Oluşturma ve Push Bildirimi (FCM) Gönderme													  
        istegi_yapan = db.query(models.Kullanici).filter(models.Kullanici.id == istek.kullanici_id).first()
        hizmet_detay = db.query(models.Hizmet).filter(models.Hizmet.id == istek.hizmet_id).first()
        arac_detay = db.query(models.Arac).filter(models.Arac.id == istek.arac_id).first()
        
        if istegi_yapan and hizmet_detay:
            # Araç ve Hizmet bilgisini log ve bildirim için zenginleştiriyoruz
            arac_bilgisi = f"{arac_detay.ozel_marka} {arac_detay.ozel_model}" if (arac_detay and arac_detay.ozel_marka) else "Kayıtlı Araç"
            bildirim_mesaji = f"Müşteri {istegi_yapan.ad_soyad}, {arac_bilgisi} aracı için '{hizmet_detay.ad}' (Talep ID: {yeni_talep.id}) talebi oluşturdu."
            
            # Log kaydına da detaylı şekilde yazdırıyoruz
            log_kaydet(db, "Yeni Talep", bildirim_mesaji, "INFO", yeni_talep.kullanici_id)

            adminler = db.query(models.Kullanici).filter(models.Kullanici.rol == "Admin").all()
            for admin in adminler:
                yeni_bildirim = models.SistemBildirimleri(
                    kullanici_id=admin.id,
                    baslik="Yeni Servis Talebi",
                    mesaj=bildirim_mesaji
                )
                db.add(yeni_bildirim)
                if admin.fcm_token:
                    mesaj_fcm = messaging.Message(
                        notification=messaging.Notification(title="Yeni Servis Talebi", body=bildirim_mesaji),
                        token=admin.fcm_token
                    )
                    messaging.send(mesaj_fcm)
        # 3. YENİ REVİZE: Eğer buraya kadar hiç hata çıkmadıysa, hem talebi hem bildirimleri aynı anda kaydediyoruz.		
        db.commit()
        db.refresh(yeni_talep)
        return yeni_talep

    except Exception as e:
        # 4. YENİ REVİZE: Hata anında yukarıdaki db.add ile hafızaya alınan tüm işlemleri iptal ediyoruz (Atomic Rollback)
        db.rollback()

        # Hatayı log tablomuza kaydediyoruz
        hata_mesaji = str(e)
        yeni_log = models.SistemLog(
            kullanici_ad_soyad="Sistem",
            seviye="ERROR",
            islem="Admin Yeni Talep Bildirimi Gönderme / Talep Oluşturma",
            detay=f"FCM Push, DB Bildirim veya Talep kaydı sırasında hata oluştu: {hata_mesaji}",
            tarih=datetime.now()
        )
        db.add(yeni_log)
        db.commit() # Sadece SistemLog tablosundaki kaydı kalıcı hale getiriyoruz

        # Senin istediğin gibi işlemi tamamen durdurup kullanıcıya 500 hatası fırlatıyoruz
        raise HTTPException(status_code=500, detail=f"İşlem sırasında bir hata oluştu ve talep iptal edildi: {hata_mesaji}")
    
    
# --- KULLANICININ SERVİS TALEPLERİ (LİSTELE, GÜNCELLE, SİL) ---
# TALEPLERİ GETİRİRKEN (Sadece A olanlar)
@app.get("/servis-talepleri/kullanici/{kullanici_id}")
def kullanici_taleplerini_getir(kullanici_id: int, db: Session = Depends(get_db)):
    talepler = db.query(models.ServisTalebi)\
                 .filter(models.ServisTalebi.kullanici_id == kullanici_id, models.ServisTalebi.kayit_durumu == 'A')\
                 .order_by(models.ServisTalebi.insert_tarihi.desc()).all()
    return talepler


# --- 1. KULLANICI TALEP GÜNCELLEME METODU ---
@app.put("/servis-talepleri/{talep_id}")
def kullanici_talep_guncelle(talep_id: int, istek: schemas.TalepGuncelleKullanici, db: Session = Depends(get_db)):
    talep = db.query(models.ServisTalebi).filter(models.ServisTalebi.id == talep_id).first()
    if not talep:
        raise HTTPException(status_code=404, detail="Talep bulunamadı")
        
    # Ortak detayları veritabanından çekiyoruz (Bildirim ve loglarda kullanmak için)
    musteri = db.query(models.Kullanici).filter(models.Kullanici.id == talep.kullanici_id).first()
    arac = db.query(models.Arac).filter(models.Arac.id == talep.arac_id).first()
    hizmet = db.query(models.Hizmet).filter(models.Hizmet.id == talep.hizmet_id).first()
    admin_kullanici = db.query(models.Kullanici).filter(models.Kullanici.rol == 'Admin').first()
    
    musteri_adi = musteri.ad_soyad if musteri else "Bilinmeyen Müşteri"
    hizmet_adi = hizmet.ad if hizmet else "Bilinmeyen Hizmet"
    
    # ARAÇ BİLGİSİNİ DOĞRU FORMATTA OLUŞTURUYORUZ (MARKALAR/MODELLER TABLOSU DESTEKLİ)
    if arac:
        if arac.ozel_marka:
            arac_bilgisi = f"{arac.ozel_marka} {arac.ozel_model}"
        elif arac.marka and arac.model:
            arac_bilgisi = f"{arac.marka.ad} {arac.model.ad}"
        else:
            arac_bilgisi = f"Araç ID: {arac.id}"
    else:
        arac_bilgisi = "Bilinmeyen Araç"

    if talep.durum == "Bekliyor":
        if istek.hizmet_id: talep.hizmet_id = istek.hizmet_id
        if istek.arac_id: talep.arac_id = istek.arac_id
        if istek.talep_tarihi: talep.talep_tarihi = istek.talep_tarihi
        if istek.adres: talep.adres = istek.adres
        if istek.notlar is not None: talep.notlar = istek.notlar
        
        # KULLANICI DÜZELTMEYİ YAPTIĞI İÇİN BAYRAĞI İNDİRİYORUZ                                                                                                                              
        talep.duzeltme_istendi_mi = False
        talep.duzeltme_notu = None
        
        # LOG KAYDINI YENİ ARAÇ BİLGİSİYLE OLUŞTUR (Object hatası çözüldü)
        log_mesaji = f"Talep ID: {talep_id} 'li Araç: {arac_bilgisi} için açılan Hizmet: {hizmet_adi} {musteri_adi} kullanıcısı tarafından düzeltildi."
        log_kaydet(db, "Talep Güncelleme", log_mesaji, "INFO", talep.kullanici_id)
        
        # EKSİK OLAN ADMİN BİLDİRİMİNİ ATIYORUZ
        if admin_kullanici:
            yeni_bildirim = models.SistemBildirimleri(
                kullanici_id=admin_kullanici.id,
                baslik="Müşteri Talebini Güncelledi",
                mesaj=log_mesaji,
                okundu_mu=False
            )
            db.add(yeni_bildirim)
            db.commit() # Hemen kaydet ki listeye düşsün
            
            if admin_kullanici.fcm_token:
                try:
                    admin_mesaj = messaging.Message(
                        notification=messaging.Notification(
                            title="Müşteri Talebini Güncelledi",
                            body=log_mesaji,
                        ),
                        token=admin_kullanici.fcm_token,
                    )
                    messaging.send(admin_mesaj)
                except Exception as e:
                    print("Admin FCM Gönderim Hatası:", e)
        
    elif talep.durum in ["Onaylandı", "İşlemde"]:
        if istek.duzeltme_istendi_mi:
            talep.duzeltme_istendi_mi = True
            talep.duzeltme_notu = istek.duzeltme_notu

            # Dinamik ve Zengin Bildirim Mesajı
            bildirim_mesaji = f"Müşteri {musteri_adi}, {arac_bilgisi} aracı için '{hizmet_adi}' (Talep ID: {talep_id}) talebine düzeltme istiyor. Not: {istek.duzeltme_notu}"

            log_kaydet(db, "Düzeltme Talebi", bildirim_mesaji, "WARNING", talep.kullanici_id)

            if admin_kullanici:
                # 1. Bildirimi Veritabanına Yaz            
                yeni_bildirim = models.SistemBildirimleri(
                    kullanici_id=admin_kullanici.id,
                    baslik="Müşteri Düzeltme Talebi",
                    mesaj=bildirim_mesaji,
                    okundu_mu=False
                )
                db.add(yeni_bildirim)
                db.commit() 
                # 2. Bildirimi Telefona At  
                if admin_kullanici.fcm_token:
                    try:
                        admin_mesaj = messaging.Message(
                            notification=messaging.Notification(
                                title="Müşteri Düzeltme İstiyor",
                                body=bildirim_mesaji,
                            ),
                            token=admin_kullanici.fcm_token,
                        )
                        messaging.send(admin_mesaj)
                    except Exception as e:
                        print("Admin FCM Gönderim Hatası:", e)

    db.commit()
    return {"mesaj": "İşlem başarılı"}


# TALEP SİLME (Soft Delete)
@app.delete("/servis-talepleri/{talep_id}")
def servis_talebi_iptal_et(talep_id: int, db: Session = Depends(get_db)):
    talep = db.query(models.ServisTalebi).filter(models.ServisTalebi.id == talep_id).first()
    if not talep:
        raise HTTPException(status_code=404, detail="Talep bulunamadı")    
    
    # YENİ İŞ KURALI: Sadece "Bekliyor" statüsündeki talepler silinebilir
    if talep.durum != "Bekliyor":
        raise HTTPException(status_code=400, detail="Sadece 'Bekliyor' durumundaki talepler iptal edilebilir.")
            
    # db.delete(talep) <--- İPTAL
    talep.kayit_durumu = 'X'
    talep.silinme_tarihi = datetime.now()
    talep.durum = "İptal Edildi" # Hem durumu güncelliyoruz hem de siliyoruz
    
    # ---------------------------------------------------------
    # YENİ: İŞLEM BAŞARIYLA İPTAL EDİLDİĞİNDE INFO LOGU AT
    log_kaydet(
        db=db, 
        islem="Servis Talebi İptali", 
        detay=f"Talep ID: {talep_id} numaralı işlem kullanıcı tarafından iptal edilip X durumuna çekildi.", 
        seviye="INFO", 
        kullanici_id=talep.kullanici_id
    )
    # ---------------------------------------------------------
    
    db.commit()
    return {"mesaj": "Talep iptal edildi ve silindi"}


# --- FİYAT GÜNCELLEME İŞLEMLERİ ---
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

@app.put("/araclar/{arac_id}", response_model=schemas.Arac)
def arac_guncelle(arac_id: int, guncel_veri: schemas.AracCreate, db: Session = Depends(get_db)):
    arac = db.query(models.Arac).filter(models.Arac.id == arac_id).first()
    
    if not arac:
        raise HTTPException(status_code=404, detail="Araç bulunamadı")
    
    # Mevcut aracin bilgilerini güncelliyoruz
    arac.marka_id = guncel_veri.marka_id
    arac.model_id = guncel_veri.model_id
    arac.yil = guncel_veri.yil
    arac.yakit_tipi = guncel_veri.yakit_tipi
    arac.kilometre = guncel_veri.kilometre
    
    db.commit()
    db.refresh(arac)
    return arac

# 2. ARAÇ SİLME (Soft Delete)
@app.delete("/araclar/{arac_id}")
def arac_sil(arac_id: int, db: Session = Depends(get_db)):
    arac = db.query(models.Arac).filter(models.Arac.id == arac_id).first()
    if not arac:
        raise HTTPException(status_code=404, detail="Araç bulunamadı")
    
    # db.delete(arac) <--- ARTIK BUNU KULLANMIYORUZ!
    arac.kayit_durumu = 'X'
    arac.silinme_tarihi = datetime.now()
    db.commit()
    
    # ---------------------------------------------------------
    # YENİ: İŞLEM BAŞARIYLA İPTAL EDİLDİĞİNDE INFO LOGU AT
    log_kaydet(
        db=db, 
        islem="Araç İptali", 
        detay=f"Araç ID: {arac_id} numaralı işlem kullanıcı tarafından iptal edilip X durumuna çekildi.", 
        seviye="INFO", 
        kullanici_id=arac.sahip_id
    )
    # ---------------------------------------------------------
    
    return {"mesaj": "Araç başarıyla silindi"}

# Silinen bir aracın geçmişte tamamlanmış bir servis talebi varsa orada marka modeli gösterebilmek için burada A veye X durumuna bakmadan tüm araçları getirdiğimiz metot.
@app.get("/araclar/{arac_id}", response_model=schemas.Arac)
def arac_getir(arac_id: int, db: Session = Depends(get_db)):
    # Aktif veya Pasif (X) fark etmeksizin aracı bulur (Geçmiş taleplerde isim göstermek için şart)
    arac = db.query(models.Arac).filter(models.Arac.id == arac_id).first()
    if not arac:
        raise HTTPException(status_code=404, detail="Araç bulunamadı")
    return arac


from pydantic import BaseModel
class SifreSifirlaIstegi(BaseModel):
    eposta: str

@app.post("/kullanicilar/sifre-sifirla_Old")
def sifre_sifirla_talep_Old(istek: SifreSifirlaIstegi, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == istek.eposta).first()
    if not kullanici:
        raise HTTPException(status_code=404, detail="Bu e-posta adresine ait bir hesap bulunamadı.")
    
    # TODO: İleride SMTP (Mail Gönderme) entegrasyonu buraya yapılacak
    return {"mesaj": "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi."}

class SifreSifirlaIstegi(BaseModel):
    eposta: str

@app.post("/kullanicilar/sifre-sifirla")
def sifre_sifirla_talep(istek: SifreSifirlaIstegi, db: Session = Depends(get_db)):
    # 1. Kullanıcıyı bul
    kullanici = db.query(models.Kullanici).filter(
        models.Kullanici.eposta == istek.eposta,
        models.Kullanici.kayit_durumu == 'A'
    ).first()
    
    if not kullanici:
        raise HTTPException(status_code=404, detail="Bu e-posta adresine ait bir hesap bulunamadı.")
    
    # 2. Geçici 6 haneli rastgele bir şifre üret
    yeni_gecici_sifre = ''.join(random.choices(string.ascii_uppercase + string.digits, k=6))
    
    # 3. Veritabanında şifreyi güncelle (İleride Hash kullanılacak)
    kullanici.sifre_hash = yeni_gecici_sifre
    db.commit()

    # 4. Şık HTML Mail İçeriği Hazırla
    mail_icerigi = f"""
    <html>
        <body style="font-family: Arial, sans-serif; color: #333; line-height: 1.6;">
            <div style="max-width: 500px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px; padding: 20px; text-align: center;">
                <h2 style="color: #00BCD4;">🚘 Kapıdan Bakım</h2>
                <p>Merhaba <b>{kullanici.ad_soyad}</b>,</p>
                <p>OtoServisApp hesabınız için şifre sıfırlama talebiniz alınmıştır. Sisteme giriş yapabilmeniz için geçici şifreniz aşağıda yer almaktadır:</p>
                
                <div style="background-color: #F9F9F9; padding: 15px; margin: 20px 0; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #111;">
                    {yeni_gecici_sifre}
                </div>
                
                <p style="font-size: 13px; color: #666;">Bu şifre ile giriş yaptıktan sonra profilinizden şifrenizi değiştirmenizi öneririz.</p>
                <p style="font-size: 12px; color: #999; margin-top: 30px;">Bu mail otomatik olarak gönderilmiştir, lütfen cevaplamayınız.</p>
            </div>
        </body>
    </html>
    """

    # 5. Maili Gönder
    gonderildi_mi = eposta_gonder(
        alici_eposta=kullanici.eposta, 
        konu="OtoServisApp - Şifre Sıfırlama Talebi", 
        icerik_html=mail_icerigi
    )

    if gonderildi_mi:
        # Başarıyla gönderildiyse logla
        log_kaydet(db, "Şifre Sıfırlama", f"{kullanici.eposta} adresine yeni geçici şifre gönderildi.", "INFO", kullanici.id)
        return {"mesaj": "Yeni şifreniz e-posta adresinize gönderildi."}
    else:
        # Geri al (Rollback) - Eğer mail gitmezse şifreyi değiştirmeyelim
        db.rollback()
        raise HTTPException(status_code=500, detail="Mail sunucusuna ulaşılamadı. Lütfen daha sonra tekrar deneyin.")

class YeniSifreBelirle(BaseModel):
    eposta: str
    yeni_sifre: str

@app.post("/kullanicilar/yeni-sifre-kaydet")
def yeni_sifre_kaydet(istek: YeniSifreBelirle, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == istek.eposta, models.Kullanici.kayit_durumu == 'A').first()
    
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı.")
        
    # Şifreyi güncelle (İleride Hash'lenecek)
    kullanici.sifre_hash = istek.yeni_sifre
    db.commit()
    return {"mesaj": "Şifreniz başarıyla güncellendi!"}

def log_kaydet(db: Session, islem: str, detay: str, seviye: str = "INFO", kullanici_id: int = None):
    try:
        yeni_log = models.SistemLog(
            seviye=seviye,
            islem=islem,
            detay=detay,
            kullanici_id=kullanici_id
        )
        db.add(yeni_log)
        db.commit()
    except Exception as e:
        pass # Log yazılırken sistem çökerse ana akışı bozma


# ==========================================
# --- ÇİFT MOTORLU SMTP MAİL ALTYAPISI ---
# ==========================================

# Şalter: Test aşamasında "GMAIL", canlıya çıkınca "NATRO" yap.
AKTIF_SMTP = "GMAIL" 


SMTP_AYARLARI = {
    "GMAIL": {
        "HOST": "smtp.gmail.com",
        "PORT": 587,
        "EMAIL": "erdogdu3434@gmail.com", # Buraya kendi Gmail'ini yaz
        "PASSWORD": "baxa dggs ybsk jnyi" # DİKKAT: Normal şifre değil, Gmail Uygulama Şifresi! 
        # Google Hesabını Yönet -> Güvenlik sekmesine gir.
        # 2 Adımlı Doğrulama'nın açık olduğundan emin ol.
        # Aynı Güvenlik sayfasında en altta "Uygulama Şifreleri" (App Passwords) kısmına gir.
        # Uygulama adına "OtoServisApp" yaz, sana 16 haneli (aralarında boşluk olan) bir şifre verecek.
    },
    "NATRO": {
        "HOST": "mail.kapidanbakim.com", # Natro sunucuları genelde mail.domain.com kullanır
        "PORT": 587,
        "EMAIL": "info@kapidanbakim.com",
        "PASSWORD": "natro_mail_sifren_buraya"
    }
}

def eposta_gonder(alici_eposta: str, konu: str, icerik_html: str):
    ayarlar = SMTP_AYARLARI[AKTIF_SMTP]
    
    msg = MIMEMultipart()
    msg['From'] = f"OtoServisApp <{ayarlar['EMAIL']}>"
    msg['To'] = alici_eposta
    msg['Subject'] = konu

    msg.attach(MIMEText(icerik_html, 'html'))

    try:
        # SMTP Sunucusuna Bağlanma ve Güvenlik (TLS) Başlatma
        server = smtplib.SMTP(ayarlar['HOST'], ayarlar['PORT'])
        server.ehlo()
        server.starttls() 
        server.login(ayarlar['EMAIL'], ayarlar['PASSWORD'])
        server.send_message(msg)
        server.quit()
        return True
    except Exception as e:
        print(f"Mail gönderme hatası: {e}")
        return False
# ==========================================


class SifreDegistirIstegi(BaseModel):
    kullanici_id: int
    eski_sifre: str
    yeni_sifre: str

@app.post("/kullanicilar/sifre-degistir")
def sifre_degistir(istek: SifreDegistirIstegi, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(
        models.Kullanici.id == istek.kullanici_id,
        models.Kullanici.kayit_durumu == 'A'
    ).first()
    
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı.")
        
    if kullanici.sifre_hash != istek.eski_sifre:
        raise HTTPException(status_code=400, detail="Mevcut şifrenizi yanlış girdiniz.")
        
    # Şifreyi güncelle ve logla
    kullanici.sifre_hash = istek.yeni_sifre
    db.commit()
    
    log_kaydet(db, "Şifre Değişimi", "Kullanıcı profil üzerinden şifresini başarıyla değiştirdi.", "INFO", kullanici.id)
    return {"mesaj": "Şifreniz başarıyla güncellendi."}

# ==========================================
# --- YÖNETİM PANELİ (ADMIN) UÇ NOKTALARI ---
# ==========================================
# --- ADMİN: AKTİF TALEPLERİ GETİR (MADDE 30 ÇÖZÜMLÜ) ---
@app.get("/admin/servis-talepleri/aktif")
def admin_aktif_talepleri_getir(db: Session = Depends(get_db)):
    siralama_kurali = case(
        (models.ServisTalebi.durum == 'Bekliyor', 1),
        (models.ServisTalebi.durum == 'Onaylandı', 2),
        (models.ServisTalebi.durum == 'İşlemde', 3),
        else_=4
    )

    talepler = db.query(models.ServisTalebi).filter(
        models.ServisTalebi.kayit_durumu == 'A',
        models.ServisTalebi.durum.in_(['Bekliyor', 'Onaylandı', 'İşlemde'])
    ).order_by(siralama_kurali, models.ServisTalebi.talep_tarihi.asc(), models.ServisTalebi.insert_tarihi.asc()).all()
    
    sonuc = []
    for t in talepler:
        kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == t.kullanici_id).first()
        arac = db.query(models.Arac).filter(models.Arac.id == t.arac_id).first()
        hizmet = db.query(models.Hizmet).filter(models.Hizmet.id == t.hizmet_id).first()
        
        arac_adi = "Silinmiş Araç"
        if arac:
            if arac.marka_id and arac.model_id:
                marka = db.query(models.Marka).filter(models.Marka.id == arac.marka_id).first()
                model = db.query(models.Model).filter(models.Model.id == arac.model_id).first()
                if marka and model:
                    arac_adi = f"{marka.ad} {model.ad}"
            else:
                arac_adi = f"{arac.ozel_marka} {arac.ozel_model}"

        talep_dict = {column.name: getattr(t, column.name) for column in t.__table__.columns}
        talep_dict["kullanici_ad_soyad"] = kullanici.ad_soyad if kullanici else "Bilinmiyor"
        talep_dict["kullanici_telefon"] = kullanici.telefon if kullanici else "Belirtilmemiş"
        talep_dict["arac_adi_tam"] = arac_adi
        
        # --- 30. MADDE: GARANTİLİ FİYAT ATAMASI ---
        # Eğer tutar 0 ise veya boşsa, hizmet tablosundaki fiyatı zorla ata (Float'a çevirerek)
        mevcut_tutar = float(t.tahmini_tutar) if t.tahmini_tutar else 0.0
        if mevcut_tutar == 0.0 and hizmet and hizmet.varsayilan_fiyat:
            talep_dict["tahmini_tutar"] = float(hizmet.varsayilan_fiyat)
        else:
            talep_dict["tahmini_tutar"] = mevcut_tutar

        sonuc.append(talep_dict)
        
    return sonuc

# --- ADMİN: GEÇMİŞ TALEPLERİ GETİR ---
@app.get("/admin/servis-talepleri/gecmis")
def admin_gecmis_talepleri_getir(db: Session = Depends(get_db)):
    talepler = db.query(models.ServisTalebi).filter(
        models.ServisTalebi.kayit_durumu == 'A',
        models.ServisTalebi.durum.in_(['Tamamlandı', 'İptal Edildi'])
    ).order_by(models.ServisTalebi.talep_tarihi.desc()).all()
    
    sonuc = []
    for t in talepler:
        kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == t.kullanici_id).first()
        arac = db.query(models.Arac).filter(models.Arac.id == t.arac_id).first()
        hizmet = db.query(models.Hizmet).filter(models.Hizmet.id == t.hizmet_id).first()
        
        arac_adi = "Silinmiş Araç"
        if arac:
            if arac.marka_id and arac.model_id:
                marka = db.query(models.Marka).filter(models.Marka.id == arac.marka_id).first()
                model = db.query(models.Model).filter(models.Model.id == arac.model_id).first()
                if marka and model:
                    arac_adi = f"{marka.ad} {model.ad}"
            else:
                arac_adi = f"{arac.ozel_marka} {arac.ozel_model}"

        talep_dict = {column.name: getattr(t, column.name) for column in t.__table__.columns}
        talep_dict["kullanici_ad_soyad"] = kullanici.ad_soyad if kullanici else "Bilinmiyor"
        talep_dict["kullanici_telefon"] = kullanici.telefon if kullanici else "Belirtilmemiş"
        talep_dict["arac_adi_tam"] = arac_adi
        
        # --- 30. MADDE ÇÖZÜMÜ ---
        mevcut_tutar = float(t.tahmini_tutar) if t.tahmini_tutar else 0.0
        if mevcut_tutar == 0.0 and hizmet and hizmet.varsayilan_fiyat:
            talep_dict["tahmini_tutar"] = float(hizmet.varsayilan_fiyat)
        else:
            talep_dict["tahmini_tutar"] = mevcut_tutar

        sonuc.append(talep_dict)
        
    return sonuc

# --- ADMİN: TALEP GÜNCELLEME (UYARI SİLİCİ) ---
from pydantic import BaseModel

class TalepAdminGuncelle(BaseModel):
    yeni_durum: str
    tahmini_tutar: float

@app.put("/admin/servis-talepleri/{talep_id}/guncelle")
def admin_talep_guncelle(talep_id: int, istek: TalepAdminGuncelle, db: Session = Depends(get_db)):
    talep = db.query(models.ServisTalebi).filter(models.ServisTalebi.id == talep_id).first()
    if not talep:
        raise HTTPException(status_code=404, detail="Talep bulunamadı")
    
    eski_durum = talep.durum
    eski_tutar = talep.tahmini_tutar
    
    talep.durum = istek.yeni_durum
    talep.tahmini_tutar = istek.tahmini_tutar
    
    # ADMİN MÜDAHALE ETTİĞİNDE VEYA DURUMU DEĞİŞTİRDİĞİNDE UYARI BAYRAĞINI TEMİZLİYORUZ
    if talep.duzeltme_istendi_mi:
        talep.duzeltme_istendi_mi = False
        talep.duzeltme_notu = None
    
    log_kaydet(
        db=db, 
        islem="Admin Talep Güncellemesi", 
        detay=f"Talep ID: {talep_id} güncellendi. Durum: {eski_durum}->{istek.yeni_durum}", 
        seviye="INFO", 
        kullanici_id=talep.kullanici_id
    )
    
    # --- YENİ BÖLÜM: BİLDİRİM VE VERİTABANI KAYDI ---
    
    # 1. BİLDİRİMİ VERİTABANINA (sistem_bildirimleri) YAZ
    yeni_bildirim = models.SistemBildirimleri(
        kullanici_id=talep.kullanici_id,
        baslik="Talebiniz Güncellendi",
        mesaj=f"Servis talebiniz '{talep.durum}' aşamasına geçmiştir.", 
        okundu_mu=False
    )
    db.add(yeni_bildirim)
    
    # Hem talebi hem de bildirimi aynı anda veritabanına kaydet (commit)
    db.commit()
    
    # 2. FIREBASE İLE TELEFONA GERÇEK BİLDİRİM FIRLAT
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == talep.kullanici_id).first()
    
    # Eğer kullanıcının FCM token'ı varsa bildirimi ateşle
    if kullanici and kullanici.fcm_token:
        try:
            message = messaging.Message(
                notification=messaging.Notification(
                    title="Kapıdan Bakım",
                    body=yeni_bildirim.mesaj,
                ),
                token=kullanici.fcm_token,
            )
            response = messaging.send(message)
            print("✅ FCM Bildirimi Başarıyla Fırlatıldı:", response)
        except Exception as e:
            print("❌ FCM Gönderim Hatası:", str(e))
            
    # Return en sonda olmalı!
    return {"mesaj": "Talep başarıyla güncellendi"}


@app.get("/admin/loglar/", tags=["Admin"])
def get_admin_logs(
    seviye: str = Query(None),
    baslangic_tarihi: date = Query(None),
    bitis_tarihi: date = Query(None),
    sayfa: int = Query(1),
    limit: int = Query(50),
    db: Session = Depends(get_db)
):
    # 1. Toplam Kayıt (Veritabanındaki filtresiz tüm log sayısı)
    toplam_kayit = db.query(models.SistemLog).count()

    # 2. Temel Sorguyu Başlat
    query = db.query(models.SistemLog)
    
    if seviye and seviye != "Tümü":
        query = query.filter(models.SistemLog.seviye == seviye)
    if baslangic_tarihi:
        query = query.filter(models.SistemLog.insert_tarihi >= datetime.combine(baslangic_tarihi, time.min))
    if bitis_tarihi:
        query = query.filter(models.SistemLog.insert_tarihi <= datetime.combine(bitis_tarihi, time.max))
        
    # 3. Filtreli Kayıt Sayısını Bul
    filtreli_kayit = query.count()

    # 4. Sayfalama (Pagination) Matematiği
    toplam_sayfa = math.ceil(filtreli_kayit / limit) if filtreli_kayit > 0 else 1
    if sayfa > toplam_sayfa:
        sayfa = toplam_sayfa

    offset_degeri = (sayfa - 1) * limit
    
    # Sadece o sayfaya ait olan limiti getir
    loglar = query.order_by(models.SistemLog.id.desc()).offset(offset_degeri).limit(limit).all()
    
    sonuc_loglar = []
    for log in loglar:
        kullanici_ad = "Sistem / Anonim"
        if log.kullanici_id:
            kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == log.kullanici_id).first()
            if kullanici:
                kullanici_ad = kullanici.ad_soyad

        sonuc_loglar.append({
            "id": log.id,
            "kullanici_ad_soyad": kullanici_ad,
            "seviye": log.seviye,
            "islem": log.islem,
            "detay": log.detay,
            "tarih": log.insert_tarihi.strftime("%d.%m.%Y %H:%M:%S") if log.insert_tarihi else ""
        })

    # C# tarafına hem bilgileri hem de listeyi paketleyip gönderiyoruz
    return {
        "toplam_kayit": toplam_kayit,
        "filtreli_kayit": filtreli_kayit,
        "toplam_sayfa": toplam_sayfa,
        "mevcut_sayfa": sayfa,
        "loglar": sonuc_loglar
    }

@app.put("/admin/talepler/{talep_id}")
def admin_talep_guncelle(talep_id: int, durum: str, tahmini_tutar: float, db: Session = Depends(get_db)):
    talep = db.query(models.ServisTalebi).filter(models.ServisTalebi.id == talep_id).first()
    if not talep:
        raise HTTPException(status_code=404, detail="Talep bulunamadı")
    
    eski_durum = talep.durum
    talep.durum = durum
    talep.tahmini_tutar = tahmini_tutar
    
    # --- BİLDİRİM TETİKLEYİCİSİ BAŞLANGICI ---
    if eski_durum != durum:
        yeni_bildirim = models.SistemBildirimleri(
            kullanici_id=talep.kullanici_id,
            baslik="Talep Durumu Güncellendi",
            mesaj=f"Servis talebinizin durumu '{durum}' olarak güncellenmiştir. Güncel tutar: {tahmini_tutar} ₺"
        )
        db.add(yeni_bildirim)
    # --- BİLDİRİM TETİKLEYİCİSİ BİTİŞİ ---

    db.commit()
    return {"mesaj": "Talep güncellendi"}

#################################################################
#################################################################
#################################################################

@app.get("/bildirimler/{kullanici_id}", response_model=List[schemas.BildirimResponse])
def bildirimleri_getir(kullanici_id: int, db: Session = Depends(get_db)):
    bildirimler = db.query(models.SistemBildirimleri)\
        .filter(models.SistemBildirimleri.kullanici_id == kullanici_id)\
        .order_by(models.SistemBildirimleri.olusturulma_tarihi.desc())\
        .all()
    return bildirimler

@app.post("/bildirimler/{bildirim_id}/okundu")
def bildirim_okundu_isaretle(bildirim_id: int, db: Session = Depends(get_db)):
    bildirim = db.query(models.SistemBildirimleri).filter(models.SistemBildirimleri.id == bildirim_id).first()
    if not bildirim:
        raise HTTPException(status_code=404, detail="Bildirim bulunamadı")
    
    bildirim.okundu_mu = True
    db.commit()
    return {"mesaj": "Bildirim okundu olarak işaretlendi"}

@app.get("/bildirimler/{kullanici_id}/okunmamis-sayi")
def okunmamis_bildirim_sayisi(kullanici_id: int, db: Session = Depends(get_db)):
    sayi = db.query(models.SistemBildirimleri).filter(
        models.SistemBildirimleri.kullanici_id == kullanici_id, 
        models.SistemBildirimleri.okundu_mu == False
    ).count()
    return sayi

@app.post("/kullanici/token-kaydet/")
def token_kaydet(istek: schemas.TokenKayitIstegi, db: Session = Depends(get_db)):
    try:
        kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == istek.kullanici_id).first()
        if not kullanici:
            raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı")
        
        kullanici.fcm_token = istek.fcm_token
        db.commit()
        
        print(f"Token güncellendi -> Kullanıcı ID: {istek.kullanici_id}")
        return {"basari": True, "mesaj": "FCM Token başarıyla kaydedildi"}
        
    except Exception as e:
        db.rollback()
        print(f"Token kayıt hatası: {str(e)}")
        raise HTTPException(status_code=500, detail="Token kaydedilemedi")