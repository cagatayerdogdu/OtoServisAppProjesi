from fastapi import FastAPI, Depends, HTTPException, APIRouter, Form, UploadFile, File
from sqlalchemy.orm import Session
import models, schemas
from database import engine, get_db
from typing import List, Dict
import logging
from logging.handlers import RotatingFileHandler
from fastapi import Request
from fastapi.responses import JSONResponse
from starlette.middleware.base import BaseHTTPMiddleware
import traceback
import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
import random
import string
from sqlalchemy import Column, Integer, String, Boolean, ForeignKey, DateTime, Float, case
import asyncio
import requests
from database import SessionLocal # Arka plan görevleri için bağımsız bir DB oturumu lazım.
from datetime import date, datetime, time, timedelta
from typing import Optional
from fastapi import Query
import math
import firebase_admin
from firebase_admin import credentials, messaging
from pydantic import BaseModel
from sqlalchemy import nullsfirst  
from fastapi.responses import HTMLResponse
from sqlalchemy import or_
# hasarlı resim ekleme importları
import io, os, uuid
from PIL import Image, ImageOps
from fastapi.staticfiles import StaticFiles
##########

models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="Oto Bakım Servisi API", version="1.0.0")

# hasarlı resim ekleme kodları
os.makedirs("HasarImg", exist_ok=True) # Klasör yoksa otomatik oluşturur
app.mount("/HasarImg", StaticFiles(directory="HasarImg"), name="HasarImg") # Klasörü dışa açar # Fotoğrafları internetten (URL üzerinden) erişilebilir hale getiriyoruz
#################

# Vitrinimizin klasörü
os.makedirs("VitrinImg", exist_ok=True)
app.mount("/VitrinImg", StaticFiles(directory="VitrinImg"), name="VitrinImg")
#################
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
    return {"mesaj": "Oto Bakım Servisi API Sistemine Hoş Geldiniz!", "durum": "Aktif"}


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
            {"ad": "Standart Periyodik Bakım", "fiyat": 4500.0, "aciklama": "Motor yağı, yağ filtresi, hava filtresi, polen filtresi değişimi ve sıvı kontroller."},
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
    
    # YENİ GÖREVLER
    asyncio.create_task(eski_fotograflari_temizle_gorevi())
    asyncio.create_task(otomatik_hatirlatma_gorevi())

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
# Madde 37: Pasif Kullanıcı Kontrolü
@app.get("/kullanicilar/pasif/{eposta}", response_model=schemas.Kullanici)
def pasif_kullanici_getir(eposta: str, db: Session = Depends(get_db)):
    # aktif_mi durumu False olan kullanıcıyı bul
    kullanici = db.query(models.Kullanici).filter(
        models.Kullanici.eposta == eposta,
        models.Kullanici.aktif_mi == False
    ).first()
    
    if not kullanici:
        raise HTTPException(status_code=404, detail="Pasif kullanıcı bulunamadı.")
    
    return kullanici

# Kullanici oluştur. Aktif pasif kontrol et.
@app.post("/kullanicilar/", response_model=schemas.Kullanici)
def kullanici_olustur_KULLANILMIYOR_OTPye_GECTIK(kullanici: schemas.KullaniciCreate, db: Session = Depends(get_db)):
    # 1. Önce bu mailde biri var mı diye bakıyoruz
    mevcut_kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == kullanici.eposta).first()
    
    if mevcut_kullanici:
        # 2. Eğer kullanıcı varsa ama hesabı Pasif (Silinmiş) ise, onu DİRİLTİYORUZ!
        # ESKİ KOD: if mevcut_kullanici.kayit_durumu == 'X':
        # YENİ REVİZE:
        if mevcut_kullanici.aktif_mi == False:
            # ESKİ KOD: mevcut_kullanici.kayit_durumu = 'A' # Yeniden Aktif
            # YENİ REVİZE:
            mevcut_kullanici.aktif_mi = True
            
            mevcut_kullanici.sifre_hash = kullanici.sifre # DÜZELTİLDİ: sifre değil sifre_hash
            mevcut_kullanici.ad_soyad = kullanici.ad_soyad
            mevcut_kullanici.telefon = kullanici.telefon
            db.commit()
            db.refresh(mevcut_kullanici)
            log_kaydet(db, "Hesap Diriltme", f"{kullanici.eposta} maili ile eski hesap yeniden aktif edildi.", "INFO", mevcut_kullanici.id)
            return mevcut_kullanici
        else:
            # 3. Hesap varsa ve zaten Aktifse, hata ver!
            raise HTTPException(status_code=400, detail="Bu e-posta adresi sistemde zaten kayıtlı!")
        
    # 2. TELEFON NUMARASI BENZERSİZLİK KONTROLÜ (YENİ)
    mevcut_telefon = db.query(models.Kullanici).filter(models.Kullanici.telefon == kullanici.telefon).first()
    if mevcut_telefon:
        # Eğer telefon numarası pasif bir hesaba aitse, o hesabı aktifleştirip yeni bilgileri atayabiliriz (isteğe bağlı).
        if mevcut_telefon.aktif_mi == False:
            mevcut_telefon.aktif_mi = True
            mevcut_telefon.sifre_hash = kullanici.sifre
            mevcut_telefon.ad_soyad = kullanici.ad_soyad
            mevcut_telefon.eposta = kullanici.eposta
            db.commit()
            db.refresh(mevcut_telefon)
            log_kaydet(db, "Hesap Diriltme (Telefon)", f"{kullanici.telefon} numaralı eski hesap yeniden aktif edildi.", "INFO", mevcut_telefon.id)
            return mevcut_telefon
        else:
            raise HTTPException(status_code=400, detail="Bu telefon numarası zaten başka bir hesaba kayıtlı. Lütfen farklı bir numara giriniz. Ayrıca girdiğiniz e-posta adresini kontrol ediniz.")
    # ------------------------------------------------------------

    # 4. Hiç kayıt yoksa sıfırdan oluştur (DÜZELTİLDİ: Manuel ve doğru eşleştirme)
    yeni_kullanici = models.Kullanici(
        ad_soyad=kullanici.ad_soyad,
        eposta=kullanici.eposta,
        telefon=kullanici.telefon,
        sifre_hash=kullanici.sifre # Veritabanındaki adı sifre_hash
    )
    # ESKİ KOD: yeni_kullanici.kayit_durumu = 'A' # Yeni kayıt aktif başlar
    # YENİ REVİZE:
    yeni_kullanici.aktif_mi = True
    
    db.add(yeni_kullanici)
    db.commit()
    db.refresh(yeni_kullanici)
    
    log_kaydet(db, "Yeni Kayıt", f"{kullanici.eposta} sisteme yeni kayıt oldu.", "INFO", yeni_kullanici.id)
    return yeni_kullanici


# Sahte e-posta ile kayıt engelleme	✅ OTP ile doğrulama
# Geçici doğrulama kodlarını saklamak için (production'da Redis önerilir)
email_dogrulama_kodlari = {}

@app.post("/kayit/eposta-dogrulama-kodu")
async def eposta_dogrulama_kodu_gonder(eposta: str = Form(...), db: Session = Depends(get_db)):
    """Kayıt öncesi e-posta adresine doğrulama kodu gönderir."""
    eposta = eposta.strip().lower()
    
    # E-posta format kontrolü
    try:
        from email_validator import validate_email, EmailNotValidError
        valid = validate_email(eposta)
        eposta = valid.email
    except:
        raise HTTPException(status_code=400, detail="Geçerli bir e-posta adresi giriniz.")
    
    # Bu e-posta zaten aktif bir hesapta kayıtlı mı?
    mevcut = db.query(models.Kullanici).filter(
        models.Kullanici.eposta == eposta,
        models.Kullanici.aktif_mi == True
    ).first()
    if mevcut:
        raise HTTPException(status_code=400, detail="Bu e-posta adresi zaten kayıtlı.")
    
    # 6 haneli rastgele kod
    kod = str(random.randint(100000, 999999))
    
    # Kodu geçici olarak sakla (10 dakika geçerli)
    email_dogrulama_kodlari[eposta] = {
        "kod": kod,
        "son_kullanma": datetime.now() + timedelta(minutes=10)
    }
    
    # Doğrulama mailini gönder
    mail_icerigi = f"""
    <html>
        <body style="font-family: Arial, sans-serif; color: #333; line-height: 1.6;">
            <div style="max-width: 500px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px; padding: 20px; text-align: center;">
                <h2 style="color: #00BCD4;">🚘 Oto Servis Bakım</h2>
                <p>Merhaba,</p>
                <p>OtoServisApp'e kayıt olmak için doğrulama kodunuz aşağıdadır:</p>
                
                <div style="background-color: #F9F9F9; padding: 15px; margin: 20px 0; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #111;">
                    {kod}
                </div>
                
                <p style="font-size: 13px; color: #666;">Bu kod 10 dakika geçerlidir.</p>
                <p style="font-size: 12px; color: #999; margin-top: 30px;">Bu mail otomatik olarak gönderilmiştir, lütfen cevaplamayınız.</p>
            </div>
        </body>
    </html>
    """
    
    if eposta_gonder(eposta, "OtoServisApp - E-posta Doğrulama Kodu", mail_icerigi):
        return {"mesaj": "Doğrulama kodu e-posta adresinize gönderildi."}
    else:
        raise HTTPException(status_code=500, detail="Doğrulama kodu gönderilemedi.")


@app.post("/kayit/dogrula-ve-kaydet")
def dogrula_ve_kaydet(
    ad_soyad: str = Form(...),
    telefon: str = Form(...),
    eposta: str = Form(...),
    sifre: str = Form(...),
    mail_istiyor_mu: bool = Form(False),
    dogrulama_kodu: str = Form(...),
    db: Session = Depends(get_db)
):
    """Doğrulama kodunu kontrol eder ve kaydı tamamlar."""
    eposta = eposta.strip().lower()
    
    # Kodu kontrol et
    kayit = email_dogrulama_kodlari.get(eposta)
    if not kayit:
        raise HTTPException(status_code=400, detail="Doğrulama kodu bulunamadı. Lütfen yeniden isteyin.")
    
    if kayit["son_kullanma"] < datetime.now():
        del email_dogrulama_kodlari[eposta]
        raise HTTPException(status_code=400, detail="Doğrulama kodunun süresi dolmuş.")
    
    if kayit["kod"] != dogrulama_kodu:
        raise HTTPException(status_code=400, detail="Doğrulama kodu hatalı.")
    
    # Kod doğru → kayıt işlemini gerçekleştir
    # 1. E-posta benzersizlik kontrolü
    mevcut_kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == eposta).first()
    if mevcut_kullanici:
        if mevcut_kullanici.aktif_mi == False:
            mevcut_kullanici.aktif_mi = True
            mevcut_kullanici.sifre_hash = sifre
            mevcut_kullanici.ad_soyad = ad_soyad
            mevcut_kullanici.telefon = telefon
            mevcut_kullanici.mail_istiyor_mu = mail_istiyor_mu
            db.commit()
            db.refresh(mevcut_kullanici)
            log_kaydet(db, "Hesap Diriltme", f"{eposta} maili ile eski hesap yeniden aktif edildi.", "INFO", mevcut_kullanici.id)
            del email_dogrulama_kodlari[eposta]
            return {"mesaj": "Hesabınız aktifleştirildi. Giriş yapabilirsiniz."}
        else:
            raise HTTPException(status_code=400, detail="Bu e-posta adresi zaten kayıtlı.")
    
    # 2. Telefon benzersizlik kontrolü
    mevcut_telefon = db.query(models.Kullanici).filter(models.Kullanici.telefon == telefon).first()
    if mevcut_telefon:
        if mevcut_telefon.aktif_mi == False:
            mevcut_telefon.aktif_mi = True
            mevcut_telefon.sifre_hash = sifre
            mevcut_telefon.ad_soyad = ad_soyad
            mevcut_telefon.eposta = eposta
            mevcut_telefon.mail_istiyor_mu = mail_istiyor_mu
            db.commit()
            db.refresh(mevcut_telefon)
            log_kaydet(db, "Hesap Diriltme (Telefon)", f"{telefon} numaralı eski hesap yeniden aktif edildi.", "INFO", mevcut_telefon.id)
            del email_dogrulama_kodlari[eposta]
            return {"mesaj": "Hesabınız aktifleştirildi. Giriş yapabilirsiniz."}
        else:
            raise HTTPException(status_code=400, detail="Bu telefon numarası zaten başka bir hesaba kayıtlı.")
    
    # 3. Yeni kayıt oluştur
    yeni_kullanici = models.Kullanici(
        ad_soyad=ad_soyad,
        eposta=eposta,
        telefon=telefon,
        sifre_hash=sifre,
        mail_istiyor_mu=mail_istiyor_mu,
        aktif_mi=True  # Doğrulandığı için direkt aktif
    )
    db.add(yeni_kullanici)
    db.commit()
    db.refresh(yeni_kullanici)
    
    log_kaydet(db, "Yeni Kayıt", f"{eposta} sisteme kayıt oldu.", "INFO", yeni_kullanici.id)
    
    # Kodu temizle
    del email_dogrulama_kodlari[eposta]
    
    return {"mesaj": "Hesabınız başarıyla oluşturuldu. Giriş yapabilirsiniz."}
# --- OTP DOĞRULAMA İLE KAYIT BİTTİ ---

# --- HESAP SİLME (SOFT DELETE) UÇ NOKTASI ---
@app.delete("/kullanicilar/{kullanici_id}")
def kullanici_sil(kullanici_id: int, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı")
        
    # ESKİ KOD: kullanici.kayit_durumu = 'X' # Soft delete!
    # YENİ REVİZE:
    kullanici.aktif_mi = False 
    
    log_kaydet(db, "Hesap Silme", f"Kullanıcı ID: {kullanici_id} hesabını sildi (Pasife Alındı).", "WARNING", kullanici_id)
    db.commit()
    return {"mesaj": "Hesabınız başarıyla silindi."}

@app.get("/kullanicilar/{kullanici_id}", response_model=schemas.Kullanici)
def kullanici_getir(kullanici_id: int, db: Session = Depends(get_db)):
    # 1. Sadece aktif kullanıcıyı getir
    kullanici = db.query(models.Kullanici).filter(
        models.Kullanici.id == kullanici_id,
        # ESKİ KOD: models.Kullanici.kayit_durumu == 'A'
        # YENİ REVİZE:
        models.Kullanici.aktif_mi == True
    ).first()
    
    if kullanici is None:
        raise HTTPException(status_code=404, detail="Kullanici bulunamadi")
        
    # 2. HAYAT KURTARAN FİLTRE: Kullanıcının silinmiş (X) kayıtlarını temizle
    # NOT: Buralardaki kayit_durumu Araç ve Talep tablosu için olduğu için DOKUNULMADI!
    kullanici.araclar = [arac for arac in kullanici.araclar if arac.kayit_durumu == 'A']
    kullanici.servis_talepleri = [talep for talep in kullanici.servis_talepleri if getattr(talep, 'kayit_durumu', 'A') == 'A']
    
    return kullanici

# Sadece pasif kullanıcıyı e-posta ile getiren özel bir uç nokta
@app.get("/kullanicilar/pasif/{eposta}", response_model=schemas.Kullanici)
def pasif_kullanici_getir(eposta: str, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == eposta, models.Kullanici.aktif_mi == False).first()
    if not kullanici:
        raise HTTPException(status_code=404, detail="Pasif kullanıcı bulunamadı.")
    return kullanici

class AktivasyonIstegi(BaseModel):
    yeni_sifre: str

@app.put("/kullanicilar/aktif-et/{kullanici_id}")
def hesabi_aktif_et(kullanici_id: int, istek: AktivasyonIstegi, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    kullanici.aktif_mi = True
    kullanici.kayit_durumu = "A"
    kullanici.sifre_hash = istek.yeni_sifre # Yeni şifreyi atıyoruz
    db.commit()
    return {"mesaj": "Hesabınız başarıyla aktif edildi."}
    

# Sadece izin durumunu almak için ufak bir şema
class MailIzniGuncelle(BaseModel):
    mail_istiyor_mu: bool
    
@app.put("/kullanici/{kullanici_id}/mail-izni")
def kullanici_mail_izni_guncelle(kullanici_id: int, istek: MailIzniGuncelle, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı")
    
    kullanici.mail_istiyor_mu = istek.mail_istiyor_mu
    db.commit()
    
    return {"mesaj": "Mail izni başarıyla güncellendi", "mail_istiyor_mu": kullanici.mail_istiyor_mu}

# ==========================================
# --- GİRİŞ YAP & LOGLAMA ---
# ==========================================
# --- YENİ REVİZE BAŞLANGICI (Giriş Loglama ve Tarih Güncelleme) ---
@app.post("/giris/", response_model=schemas.Kullanici)
def giris_yap(giris_bilgileri: schemas.KullaniciGiris, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(
        models.Kullanici.eposta == giris_bilgileri.eposta
        # ESKİ KOD: models.Kullanici.kayit_durumu == 'A' -> İptal edildi, aktif_mi kontrolü zaten aşağıda var.
    ).first()
    
    if not kullanici or kullanici.sifre_hash != giris_bilgileri.sifre:
        raise HTTPException(status_code=401, detail="E-posta veya şifre hatali")
        
    # YENİ REVİZE: kayit_durumu iptal olduğu için "soft_delete" ve "askıya alma" burada birleşti.
    if not kullanici.aktif_mi:
        raise HTTPException(status_code=403, detail=f"Hesabınız pasif durumdadır.\nLütfen profil ekranından aktif ederek tekrar giriş yapmayı deneyiniz.")
        
    # YENİ: Son giriş tarihini güncelliyoruz
    kullanici.son_giris_tarihi = datetime.now()
    db.commit()
    
    # YENİ: Sistem loglarına kaydı atıyoruz
    log_kaydet(db, "Sisteme Giriş", f"{kullanici.ad_soyad} uygulamaya giriş yaptı.", "INFO", kullanici.id)

    # DİKKAT: Araç ve Talep tablosundaki kayit_durumu'na dokunulmadı!
    kullanici.araclar = [arac for arac in kullanici.araclar if arac.kayit_durumu == 'A']
    kullanici.servis_talepleri = [talep for talep in kullanici.servis_talepleri if getattr(talep, 'kayit_durumu', 'A') == 'A']
    return kullanici
# --- YENİ REVİZE BİTİŞİ ---


# ==========================================
# --- YENİ: MÜŞTERİ TAKİP & CRM MODÜLÜ ---
# ==========================================
@app.get("/admin/kullanici-takip")
def kullanici_takip_listesi(
    sayfa: int = 1, 
    sayfa_boyutu: int = 15, 
    db: Session = Depends(get_db)
):
    atla = (sayfa - 1) * sayfa_boyutu

    # Sadece Müşterileri getir, aktif olanlar
    query = db.query(models.Kullanici).filter(
        models.Kullanici.rol == "Musteri",
        # ESKİ KOD: models.Kullanici.kayit_durumu == 'A'
        # YENİ REVİZE:
        models.Kullanici.aktif_mi == True
    )
    
    # MySQL uyumlu sıralama: önce NULL olanlar (hiç giriş yapmamış) en üstte,
    # sonra son_giris_tarihi'ne göre artan (eskiden yeniye)
    # SQLAlchemy 1.4+ için doğru sözdizimi
    query = query.order_by(
        case(
            (models.Kullanici.son_giris_tarihi == None, 0),
            else_=1
        ).asc(),
        models.Kullanici.son_giris_tarihi.asc()
    )

    toplam_kayit = query.count()
    kullanicilar = query.offset(atla).limit(sayfa_boyutu).all()

    liste = []
    for k in kullanicilar:
        kac_gun = None
        son_giris_str = None
        if k.son_giris_tarihi:
            fark = (datetime.now() - k.son_giris_tarihi).days
            kac_gun = fark
            son_giris_str = k.son_giris_tarihi.strftime("%d.%m.%Y %H:%M")
        
        liste.append({
            "id": k.id,
            "ad_soyad": k.ad_soyad,
            "eposta": k.eposta,
            "son_giris_tarihi": son_giris_str,
            "kac_gun_oldu": kac_gun,    # diff.days if k.son_giris_tarihi else None, bunu denemedim.
            "mail_istiyor_mu": k.mail_istiyor_mu,
            "son_hatirlatma_tarihi": k.son_hatirlatma_tarihi.strftime("%Y-%m-%d %H:%M") if k.son_hatirlatma_tarihi else None
        })

    toplam_sayfa = (toplam_kayit + sayfa_boyutu - 1) // sayfa_boyutu if toplam_kayit > 0 else 1

    return {
        "liste": liste,
        "toplam_kayit": toplam_kayit,
        "toplam_sayfa": toplam_sayfa
    }


class ManuelHatirlatmaIstegi(BaseModel):
    ozel_mesaj: str

###############################################
########### ADMİN TARAFI İÇİN BLOK ############
###############################################
@app.post("/admin/kullanici-takip/{kullanici_id}/hatirlatma-gonder")
def manuel_hatirlatma_gonder(kullanici_id: int, istek: ManuelHatirlatmaIstegi, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı")
        
    # KVKK Kontrolü
    if not kullanici.mail_istiyor_mu:
        raise HTTPException(status_code=403, detail="Kullanıcı e-posta bildirimlerini kapattığı için mail atılamaz (KVKK).")

    # Kurumsal İmza Ekleniyor
    # Bu e-postayı almak istemiyorsanız <a href="https://api.otobakimservisi.com/kvkk/mail-iptal/{kullanici.id}">buraya tıklayarak</a> abonelikten çıkabilirsiniz.
    # Natro ya taşıdığımda linki üstteki gibi güncellicez şimdi localhostta gönderiyoruz.
    mail_icerigi = f"""
    <html>
        <body style="font-family: Arial, sans-serif; color: #333; line-height: 1.6;">
            <p>Selamlar <b>{kullanici.ad_soyad}</b>,</p>
            <p>{istek.ozel_mesaj}</p>
            <br>
            <p>Sizi tekrar aramızda görmekten mutluluk duyarız. Araç bakımlarınız için uygulamamızı ziyaret edebilirsiniz.</p>
            <br>
            <p>Hayırlı günler dileriz,<br><b>Oto Servis Bakım Yönetimi</b></p>
            <hr>
            <p style="font-size: 11px; color: #999;">
                Bu e-postayı almak istemiyorsanız <a href="http://136.115.53.49:8000/kvkk/mail-iptal/{kullanici.id}">buraya tıklayarak</a> abonelikten çıkabilirsiniz.
            </p>
        </body>
    </html>
    """
    
    # Mail Gönder
    basarili_mi = eposta_gonder(kullanici.eposta, "Sizi Özledik! - Oto Servis Bakım", mail_icerigi)
    
    if basarili_mi:
        # Spam engeli için son hatırlatma tarihini kaydet
        kullanici.son_hatirlatma_tarihi = datetime.now()
        db.commit()
        log_kaydet(db, "Manuel Hatırlatma", f"{kullanici.eposta} adresine geri dönüş maili atıldı.", "INFO", kullanici.id)
        
        # Telefona FCM Push Gönder (Push KVKK'ya girmez, cihaz iznine tabidir, direkt yollayabiliriz)
        if kullanici.fcm_token:
            try:
                mesaj_fcm = messaging.Message(
                    notification=messaging.Notification(title="Sizi Özledik!", body="Uygulamamıza uzun zamandır girmediniz, sizi bekliyoruz!"),
                    token=kullanici.fcm_token
                )
                messaging.send(mesaj_fcm)
            except:
                pass

        return {"mesaj": "Mail ve Bildirim başarıyla gönderildi."}
    else:
        raise HTTPException(status_code=500, detail="Mail gönderilemedi, SMTP ayarlarını kontrol edin.")

###############################################
# YENİ REVİZE: Daha şık HTML arayüzü ile yapılandırılmış iptal sayfası
# KVKK Mail İptal Endpoint'i (Müşteri maildeki linke tıklayınca burası çalışır)
@app.get("/kvkk/mail-iptal/{kullanici_id}") # response_class'ı buradan kaldır, return içinde verelim garanti olsun
def mail_abonelik_iptal(kullanici_id: int, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    
    if not kullanici:
        return HTMLResponse(content="<h2 style='color:red; text-align:center; margin-top:50px;'>Kullanıcı bulunamadı.</h2>", status_code=404)
    
    kullanici.mail_istiyor_mu = False
    db.commit()
    
    html_icerik = f"""
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=yes">
    <title>Abonelik İptali</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Roboto, system-ui, -apple-system, sans-serif;
            background: linear-gradient(145deg, #F8FAFC 0%, #E2E8F0 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
            margin: 0;
        }}
        .card {{
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(8px);
            -webkit-backdrop-filter: blur(8px);
            max-width: 500px;
            width: 100%;
            padding: 40px 30px;
            border-radius: 36px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            border: 1px solid rgba(255, 255, 255, 0.5);
            text-align: center;
            transition: all 0.2s ease;
        }}
        .icon {{
            font-size: 64px;
            line-height: 1.2;
            margin-bottom: 16px;
        }}
        h2 {{
            font-size: 28px;
            font-weight: 700;
            color: #0F172A;
            margin-bottom: 16px;
            letter-spacing: -0.01em;
        }}
        .success-badge {{
            background: #10B981;
            color: white;
            display: inline-block;
            padding: 6px 18px;
            border-radius: 40px;
            font-size: 15px;
            font-weight: 600;
            margin-bottom: 24px;
            letter-spacing: 0.3px;
            box-shadow: 0 4px 8px rgba(16, 185, 129, 0.2);
        }}
        .message {{
            font-size: 17px;
            color: #1E293B;
            line-height: 1.6;
            margin-bottom: 28px;
            font-weight: 500;
        }}
        .name {{
            font-weight: 700;
            color: #0EA5E9;
        }}
        .info-box {{
            background: #F1F5F9;
            border-radius: 24px;
            padding: 22px 18px;
            margin: 24px 0 20px;
            border-left: 4px solid #00BCD4;
            text-align: left;
        }}
        .info-box p {{
            font-size: 16px;
            color: #334155;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        .info-box .small {{
            font-size: 14px;
            color: #64748B;
            margin-top: 12px;
            line-height: 1.5;
        }}
        .footer {{
            margin-top: 24px;
            font-size: 13px;
            color: #94A3B8;
            border-top: 1px dashed #CBD5E1;
            padding-top: 22px;
        }}
        .btn {{
            display: inline-block;
            background: #00BCD4;
            color: white;
            font-weight: 600;
            padding: 14px 28px;
            border-radius: 60px;
            text-decoration: none;
            font-size: 16px;
            margin-top: 10px;
            box-shadow: 0 10px 15px -3px rgba(0, 188, 212, 0.2);
            transition: all 0.15s;
            border: none;
        }}
        .btn:hover {{
            background: #0097A7;
            transform: scale(1.02);
        }}
        @media (max-width: 480px) {{
            .card {{ padding: 30px 20px; }}
            h2 {{ font-size: 24px; }}
            .message {{ font-size: 16px; }}
        }}
    </style>
</head>
<body>
    <div class="card">
        <div class="icon">📬</div>
        <h2>Abonelikten Çıkıldı</h2>
        <div class="success-badge">✓ İşlem Başarılı</div>
        <div class="message">
            Sayın <span class="name">{kullanici.ad_soyad}</span>,<br>
            e-posta bildirimleri başarıyla kapatıldı.
        </div>
        <div class="info-box">
            <p>🔕 Artık hatırlatma e-postaları almayacaksınız.</p>
            <div class="small">
                💡 Fikrinizi değiştirirseniz, <strong>uygulamadaki Hesabım / Profil</strong> bölümünden<br> 
                “E-posta Bildirimleri” ayarını tekrar açabilirsiniz.
            </div>
        </div>
        <div class="footer">
            Oto Servis Bakım<br>
            <span style="font-size:12px">© 2026 • Tüm hakları saklıdır</span>
        </div>
    </div>
</body>
</html>
"""

    # Burası ÇOK ÖNEMLİ: media_type'ı zorla veriyoruz ki tarayıcı düz yazı sanmasın
    return HTMLResponse(content=html_icerik, status_code=200, media_type="text/html")

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
    # DİKKAT: Araç tablosunda kayit_durumu KORUNDU
    araclar = db.query(models.Arac).filter(
        models.Arac.sahip_id == kullanici_id,
        models.Arac.kayit_durumu == 'A'
    ).all()
    return araclar

# --- MADDE 28: ARAÇTA AKTİF TALEP VAR MI KONTROLÜ ---
@app.get("/araclar/{arac_id}/aktif-talep-kontrol")
def aktif_talep_kontrol(arac_id: int, db: Session = Depends(get_db)):
    # DİKKAT: Talep tablosunda kayit_durumu KORUNDU
    aktif_talep = db.query(models.ServisTalebi).filter(
        models.ServisTalebi.arac_id == arac_id,
        models.ServisTalebi.durum.in_(['Bekliyor', 'Onaylandı', 'İşlemde']),
        models.ServisTalebi.kayit_durumu == 'A'
    ).first()
    
    return {"aktif_talep_var": aktif_talep is not None}

# --- MADDE 28: GÜVENLİ ARAÇ GÜNCELLEME ---
@app.put("/araclar/{arac_id}")
def arac_guncelle(arac_id: int, arac_data: schemas.AracCreate, db: Session = Depends(get_db)):
    # DİKKAT: Araç tablosunda kayit_durumu KORUNDU
    mevcut_arac = db.query(models.Arac).filter(models.Arac.id == arac_id, models.Arac.kayit_durumu == 'A').first()
    if not mevcut_arac:
        raise HTTPException(status_code=404, detail="Araç bulunamadı.")

    # DİKKAT: Talep tablosunda kayit_durumu KORUNDU
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
        # --- MADDE 55 KONTROLÜ BAŞLANGICI ---
        aktif_talepler = db.query(models.ServisTalebi).filter(
            models.ServisTalebi.arac_id == istek.arac_id,
            models.ServisTalebi.durum.in_(["Bekliyor", "Onaylandı", "İşlemde"]),
            models.ServisTalebi.kayit_durumu == 'A'
        ).all()

        if aktif_talepler:
            # Bu araç için aktif olan taleplerin hizmet id'lerini bulalım
            aktif_hizmetler = [t.hizmet_id for t in aktif_talepler]
            
            if istek.hizmet_id in aktif_hizmetler:
                # Kullanıcı zaten aktif olan aynı hizmeti tekrar oluşturmaya çalışıyor -> Soru soralım
                raise HTTPException(status_code=400, detail="Bu araç için mevcut bir talebiniz var. Yeni bir hizmet seçerek yeni bir talep açmak ister misiniz?")
            # Eğer farklı bir hizmet ise kod hiçbir şeye takılmadan aşağıya devam edip yeni talebi oluşturacak.
        # --- MADDE 55 KONTROLÜ BİTİŞİ ---
        
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

    # YENİ REVİZE (HATA YUTULMASINI ENGELLEYEN BLOK): 
    # Bizim bilerek fırlattığımız 400 hatalarının aşağıdaki Exception'a düşüp 500 olmasını engeller.
    except HTTPException as http_exc:
        raise http_exc

    except Exception as e:
        # 4. YENİ REVİZE: Hata anında yukarıdaki db.add ile hafızaya alınan tüm işlemleri iptal ediyoruz (Atomic Rollback)
        db.rollback()

        # Hatayı log tablomuza kaydediyoruz
        hata_mesaji = str(e)
        yeni_log = models.SistemLog(
            # kullanici_ad_soyad="Sistem",
            kullanici_id= istek.kullanici_id,
            seviye="ERROR",
            islem="Yeni Talep Oluşturma",
            detay=f"FCM Push, DB Bildirim veya Talep kaydı sırasında hata oluştu: {hata_mesaji}",
            # insert_tarihi=datetime.now() gereksiz çünkü tabloda server_default=func.now() var.
        )
        db.add(yeni_log)
        db.commit() # Sadece SistemLog tablosundaki kaydı kalıcı hale getiriyoruz

        # Senin istediğin gibi işlemi tamamen durdurup kullanıcıya 500 hatası fırlatıyoruz.
        raise HTTPException(status_code=500, detail=f"İşlem sırasında bir hata oluştu ve talep iptal edildi: {hata_mesaji}")
    
    
# --- KULLANICININ SERVİS TALEPLERİ (LİSTELE, GÜNCELLE, SİL) ---
# TALEPLERİ GETİRİRKEN (Sadece A olanlar)
# @app.get("/servis-talepleri/kullanici/{kullanici_id}")
# def kullanici_taleplerini_getir(kullanici_id: int, db: Session = Depends(get_db)):
    # DİKKAT: Talep tablosunda kayit_durumu KORUNDU
#     talepler = db.query(models.ServisTalebi)\
#                  .filter(models.ServisTalebi.kullanici_id == kullanici_id, models.ServisTalebi.kayit_durumu == 'A')\
#                  .order_by(models.ServisTalebi.insert_tarihi.desc()).all()
#     return talepler

# ------------------------------------------------------------
# KULLANICI TALEPLERİ (SAYFALI + FİLTRELİ)
# ------------------------------------------------------------
@app.get("/servis-talepleri/kullanici/{kullanici_id}")
def kullanici_taleplerini_getir(
    kullanici_id: int,
    skip: int = Query(0, ge=0),
    limit: int = Query(20, ge=1, le=100),
    durum: Optional[str] = Query(None),
    arama: Optional[str] = Query(None),
    db: Session = Depends(get_db)
):
    # Ana sorgu
    query = db.query(models.ServisTalebi)\
        .filter(models.ServisTalebi.kullanici_id == kullanici_id,
                models.ServisTalebi.kayit_durumu == 'A')

    if durum and durum != "Tümü":
        query = query.filter(models.ServisTalebi.durum == durum)

    if arama:
        arama = arama.lower()
        # Hizmet adı
        query = query.join(models.Hizmet, models.ServisTalebi.hizmet_id == models.Hizmet.id, isouter=True)\
                     .join(models.Arac, models.ServisTalebi.arac_id == models.Arac.id, isouter=True)

        # Araç marka/model bilgisi için ek join'ler
        query = query.outerjoin(models.Marka, models.Arac.marka_id == models.Marka.id)\
                     .outerjoin(models.Model, models.Arac.model_id == models.Model.id)

        query = query.filter(
            (models.Hizmet.ad.ilike(f"%{arama}%")) |
            (models.Arac.ozel_marka.ilike(f"%{arama}%")) |
            (models.Arac.ozel_model.ilike(f"%{arama}%")) |
            (models.Marka.ad + " " + models.Model.ad).ilike(f"%{arama}%")
        )

    # Toplam kayıt sayısı
    toplam_kayit = query.count()

    # Sıralama: durum önceliği + ID azalan
    siralama = case(
        (models.ServisTalebi.durum == 'Bekliyor', 1),
        (models.ServisTalebi.durum == 'Onaylandı', 2),
        (models.ServisTalebi.durum == 'İşlemde', 3),
        (models.ServisTalebi.durum == 'Tamamlandı', 4),
        (models.ServisTalebi.durum == 'İptal Edildi', 5),
        else_=6
    )

    talepler = query.order_by(siralama, models.ServisTalebi.id.desc())\
                    .offset(skip).limit(limit).all()

    return {
        "talepler": talepler,
        "toplam_kayit": toplam_kayit
    }


# --- 1. KULLANICI TALEP GÜNCELLEME METODU ---
@app.put("/servis-talepleri/{talep_id}")
def kullanici_talep_guncelle(talep_id: int, istek: schemas.TalepGuncelleKullanici, db: Session = Depends(get_db)):
    # İlgili talebi veritabanından çekiyoruz
    talep = db.query(models.ServisTalebi).filter(models.ServisTalebi.id == talep_id).first()
    if not talep:
        raise HTTPException(status_code=404, detail="Talep bulunamadı")
        
    # Ortak detayları veritabanından çekiyoruz (Bildirim ve loglarda kullanmak için)
    musteri = db.query(models.Kullanici).filter(models.Kullanici.id == talep.kullanici_id).first()
    arac = db.query(models.Arac).filter(models.Arac.id == talep.arac_id).first()
    hizmet = db.query(models.Hizmet).filter(models.Hizmet.id == talep.hizmet_id).first()
    admin_kullanici = db.query(models.Kullanici).filter(models.Kullanici.rol == 'Admin').first()
    
    # İsimleri ve hizmet adlarını ayarlıyoruz
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
        
        # --- YENİ REVİZE BAŞLANGICI (MADDE 63) ---
        # Hangi alanların değiştiğini takip etmek için boş bir liste oluşturuyoruz
        degisen_alanlar = []

        # Eski atama kodlarını yoruma aldık (Çalışan kodları silmemek adına referans bırakıldı):
        # if istek.hizmet_id: talep.hizmet_id = istek.hizmet_id
        # if istek.arac_id: talep.arac_id = istek.arac_id
        # if istek.talep_tarihi: talep.talep_tarihi = istek.talep_tarihi
        # if istek.adres: talep.adres = istek.adres
        # if istek.notlar is not None: talep.notlar = istek.notlar
        
       # Yerine hem atama yapıp hem de değişikliği tespit eden kodları ekledik:
        if istek.hizmet_id and talep.hizmet_id != istek.hizmet_id:
            yeni_hizmet = db.query(models.Hizmet).filter(models.Hizmet.id == istek.hizmet_id).first()
            degisen_alanlar.append(f"Hizmet ({yeni_hizmet.ad if yeni_hizmet else 'Bilinmeyen'})")
            talep.hizmet_id = istek.hizmet_id
            
        if istek.arac_id and talep.arac_id != istek.arac_id:
            yeni_arac = db.query(models.Arac).filter(models.Arac.id == istek.arac_id).first()
            if yeni_arac:
                arac_detay = f"{yeni_arac.ozel_marka} {yeni_arac.ozel_model}" if yeni_arac.ozel_marka else f"{yeni_arac.marka.ad} {yeni_arac.model.ad}"
            else:
                arac_detay = "Bilinmeyen Araç"
            degisen_alanlar.append(f"Araç Bilgisi ({arac_detay})")
            talep.arac_id = istek.arac_id
            
        if istek.talep_tarihi and str(talep.talep_tarihi) != str(istek.talep_tarihi):
            degisen_alanlar.append(f"Randevu Tarihi ({istek.talep_tarihi})")
            talep.talep_tarihi = istek.talep_tarihi
            
        if istek.adres and talep.adres != istek.adres:
            degisen_alanlar.append(f"Adres ({istek.adres})")
            talep.adres = istek.adres
            
        if istek.notlar is not None and talep.notlar != istek.notlar:
            degisen_alanlar.append(f"Müşteri Notu ({istek.notlar})")
            talep.notlar = istek.notlar
        # --- YENİ REVİZE BİTİŞİ ---
        
        # KULLANICI DÜZELTMEYİ YAPTIĞI İÇİN BAYRAĞI İNDİRİYORUZ                                                                          
        talep.duzeltme_istendi_mi = False
        talep.duzeltme_notu = None
        
        # SADECE DEĞİŞİKLİK VARSA BİLDİRİM VE LOG İŞLEMİ YAP
        if degisen_alanlar:
            # Değişen alanları virgülle ayırarak metne dönüştürüyoruz
            degisiklik_metni = ", ".join(degisen_alanlar)
            
            # Eski Log Mesajı yoruma alındı:
            # log_mesaji = f"Talep ID: {talep_id} 'li Araç: {arac_bilgisi} için açılan Hizmet: {hizmet_adi} {musteri_adi} kullanıcısı tarafından düzeltildi."
            
            # Yeni Log Mesajı (Değişen detayları içeriyor):
            log_mesaji = f"(Talep ID: {talep_id} ) '{hizmet_adi}' için {musteri_adi} şu detayları güncelledi: {degisiklik_metni}."
            
            # Veritabanına logu kaydediyoruz
            log_kaydet(db, "Talep Güncelleme", log_mesaji, "INFO", talep.kullanici_id)
            
            # EKSİK OLAN ADMİN BİLDİRİMİNİ ATIYORUZ
            if admin_kullanici:
                # Bildirimi veritabanında oluşturuyoruz
                yeni_bildirim = models.SistemBildirimleri(
                    kullanici_id=admin_kullanici.id,
                    baslik="Müşteri Talebini Güncelledi",
                    mesaj=log_mesaji,
                    okundu_mu=False
                )
                db.add(yeni_bildirim)
                db.commit() # Hemen kaydet ki listeye düşsün
                
                # Push Notification (FCM) gönderiyoruz
                if admin_kullanici.fcm_token:
                    try:
                        admin_mesaj = messaging.Message(
                            notification=messaging.Notification(
                                title="Talep Detayları Değişti",
                                body=log_mesaji,
                            ),
                            token=admin_kullanici.fcm_token,
                        )
                        messaging.send(admin_mesaj)
                    except Exception as e:
                        print("Admin FCM Gönderim Hatası:", e)
        else:
            # Hiçbir şey değişmemişse bile değişiklikleri (varsa bayrak inmesi vs.) onayla
            db.commit()
        
    elif talep.durum in ["Onaylandı", "İşlemde"]:
        # Müşteri düzeltme istiyorsa bayrağı ve notu güncelliyoruz
        if istek.duzeltme_istendi_mi:
            talep.duzeltme_istendi_mi = True
            talep.duzeltme_notu = istek.duzeltme_notu

            # Dinamik ve Zengin Bildirim Mesajı oluşturuyoruz
            bildirim_mesaji = f"Müşteri {musteri_adi}, {arac_bilgisi} aracı için '{hizmet_adi}' (Talep ID: {talep_id}) talebine düzeltme istiyor. Not: {istek.duzeltme_notu}"

            # Log kaydını tutuyoruz
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

# TALEP SİLME (Soft Delete) -- Kullanıcı kendi talebini iptal ettiğinde.
@app.delete("/servis-talepleri/{talep_id}")
def servis_talebi_iptal_et(talep_id: int, db: Session = Depends(get_db)):
    talep = db.query(models.ServisTalebi).filter(models.ServisTalebi.id == talep_id).first()
    if not talep:
        raise HTTPException(status_code=404, detail="Talep bulunamadı")    
    
    # YENİ İŞ KURALI: Sadece "Bekliyor" statüsündeki talepler silinebilir
    if talep.durum != "Bekliyor":
        raise HTTPException(status_code=400, detail="Sadece 'Bekliyor' durumundaki talepler iptal edilebilir.")
            
    # Tüm tarihleri ve ID'leri eksiksiz dolduruyoruz
    zaman_simdi = datetime.now()
    talep.kayit_durumu = 'X'
    talep.durum = "İptal Edildi"
    talep.silinme_tarihi = zaman_simdi
    talep.guncelleme_tarihi = zaman_simdi
    talep.tamamlanma_tarihi = None
    # Kullanıcı kendi sildiği için iptal eden kendisidir
    talep.iptal_eden_id = talep.kullanici_id
        
    # ---------------------------------------------------------
    # YENİ: İŞLEM BAŞARIYLA İPTAL EDİLDİĞİNDE INFO LOGU AT
    log_kaydet(
        db=db, 
        islem="Servis Talebi İptali", 
        detay=f"Talep ID: {talep_id} numaralı işlem {talep.iptal_eden_id} kullanıcısı tarafından iptal edilip X durumuna çekildi.", 
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
    # DİKKAT: Araç tablosunda kayit_durumu YAPISI KORUNUYOR
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


#@app.post("/kullanicilar/sifre-sifirla_Old")
#def sifre_sifirla_talep_Old(istek: SifreSifirlaIstegi, db: Session = Depends(get_db)):
#    kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == istek.eposta).first()
#    if not kullanici:
#        raise HTTPException(status_code=404, detail="Bu e-posta adresine ait bir hesap bulunamadı.")
    
    # TODO: İleride SMTP (Mail Gönderme) entegrasyonu buraya yapılacak
#    return {"mesaj": "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi."}

class SifreSifirlaIstegi(BaseModel):
    eposta: str

@app.post("/kullanicilar/sifre-sifirla")
def sifre_sifirla_talep(istek: SifreSifirlaIstegi, db: Session = Depends(get_db)):
    # 1. Kullanıcıyı bul
    kullanici = db.query(models.Kullanici).filter(
        models.Kullanici.eposta == istek.eposta,
        # ESKİ KOD: models.Kullanici.kayit_durumu == 'A'
        # YENİ REVİZE:
        models.Kullanici.aktif_mi == True
    ).first()
    
    if not kullanici:
        raise HTTPException(status_code=404, detail="Bu e-posta adresine ait bir hesap bulunamadı veya hesabınız pasif durumdadır.")
    
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
                <h2 style="color: #00BCD4;">🚘 Oto Bakım Servisi</h2>
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
        # --- YENİ: Kullanıcıya Push Bildirimi Gönder ---
        if kullanici.fcm_token:
            try:
                from firebase_admin import messaging
                mesaj = messaging.Message(
                    notification=messaging.Notification(
                        title="Şifre Sıfırlama",
                        # body="Şifre sıfırlama talimatları e-posta adresinize gönderildi."                        
                        body=f"Şifreniz e-posta adresinize gönderildi. Yeni geçici şifreniz: {yeni_gecici_sifre}"
                    ),
                    token=kullanici.fcm_token
                )
                messaging.send(mesaj)
            except Exception as push_err:
                print(f"Push bildirimi gönderilemedi: {push_err}")
                
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
    # ESKİ KOD: kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == istek.eposta, models.Kullanici.kayit_durumu == 'A').first()
    # YENİ REVİZE:
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.eposta == istek.eposta, models.Kullanici.aktif_mi == True).first()
    
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı veya hesabınız pasif.")
        
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
        # ESKİ KOD: models.Kullanici.kayit_durumu == 'A'
        # YENİ REVİZE:
        models.Kullanici.aktif_mi == True
    ).first()
    
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı veya hesabınız pasif.")
        
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

    # DİKKAT: Talep tablosunda kayit_durumu KORUNDU
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

# ---------------------------------------------------------
# --- ADMİN: GEÇMİŞ TALEPLERİ GETİR (VERİ ÇEKME) ---
# ---------------------------------------------------------
#@app.get("/admin/servis-talepleri/gecmis")
# def admin_gecmis_talepleri_getir(db: Session = Depends(get_db)):
    # Tamamlanmış ve İptal Edilmiş talepleri çekiyoruz
    # Sorgu performansını artırmak için gerekli filtrelemeyi yapıyoruz
#     talepler = db.query(models.ServisTalebi).filter(
       # models.ServisTalebi.kayit_durumu == 'A', # burada A dakileri çektiğimiz de
       # müşterinin iptal taleplerini göremiyorduk. Bu filtreyi kaldırdım.
       # burası için iptal eden user eklememiz gerekecek.  
#         models.ServisTalebi.durum.in_(['Tamamlandı', 'İptal Edildi'])
#     ).order_by(models.ServisTalebi.talep_tarihi.desc()).all()
    
#     sonuc = []
#     for t in talepler:                        
        # İlişkili verileri çekiyoruz
#         kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == t.kullanici_id).first()
#         arac = db.query(models.Arac).filter(models.Arac.id == t.arac_id).first()
#         hizmet = db.query(models.Hizmet).filter(models.Hizmet.id == t.hizmet_id).first()
        
#         arac_adi = "Silinmiş Araç"
#         if arac:
#             if arac.marka_id and arac.model_id:
#                 marka = db.query(models.Marka).filter(models.Marka.id == arac.marka_id).first()
#                 model = db.query(models.Model).filter(models.Model.id == arac.model_id).first()
#                 if marka and model:
#                     arac_adi = f"{marka.ad} {model.ad}"
#             else:
#                 arac_adi = f"{arac.ozel_marka} {arac.ozel_model}"        
        
        # 1. Mevcut kolonları sözlüğe aktar
#         talep_dict = {c.name: getattr(t, c.name) for c in t.__table__.columns}
#         
#         # Tarihleri güvenli şekilde ISO formatına çevir (None ise None bırak)
#         for key, value in talep_dict.items():
#             if isinstance(value, (datetime, date)):
#                 talep_dict[key] = value.isoformat() if value else None
#         
#         # talep_tarihi C#'ta string olduğu için onu özel olarak string formatında eziyoruz (Çökme engellendi)
#         if t.talep_tarihi:
#             talep_dict["talep_tarihi"] = t.talep_tarihi.strftime("%Y-%m-%d %H:%M")
#                     
#         # 2. Kullanıcı bilgilerini yapılandır
#         talep_dict["kullanici_ad_soyad"] = kullanici.ad_soyad if kullanici else "Bilinmiyor"
#         talep_dict["kullanici_telefon"] = kullanici.telefon if kullanici else "Belirtilmemiş"
#         talep_dict["arac_adi_tam"] = arac_adi
# 
#         # 3. İptal eden bilgisini yapılandır
#         iptal_eden_isim = "İptal bilgisi yok."
#         if t.iptal_eden_id: # is not None:
#             iptal_kisi = db.query(models.Kullanici).filter(models.Kullanici.id == t.iptal_eden_id).first()
#             if iptal_kisi:
#                 iptal_eden_isim = iptal_kisi.ad_soyad
#         talep_dict["iptal_eden_ad_soyad"] = iptal_eden_isim
# 
#         # Tarih alanlarını C# DateTime? tipine uygun hale getir
#         # 4. ADIM: Tarih Kurtarma Operasyonu (C# tarafının beklediği isimlerle)
#         # Eğer iptal edildiyse, iptal tarihini ve tamamlanma tarihini dolduruyoruz        
#         # Eğer veritabanında tamamlanma_tarihi NULL ise, eski kayıtların boş görünmemesi 
#         # için guncelleme veya silinme tarihini baz alıyoruz.		
#         # Tarih Garantisi: Boş gelmesini engelle					   
#         # if t.durum == "İptal Edildi":
#         #    talep_dict["tamamlanma_tarihi"] = t.silinme_tarihi or t.guncelleme_tarihi
#         #elif not t.tamamlanma_tarihi:
#         #    talep_dict["tamamlanma_tarihi"] = t.guncelleme_tarihi
#         
#         # 4. Tamamlanma/İptal tarihi NULL ise guncelleme tarihini bas
#                 # Güvenli tarih atamaları (None kontrolü)
#         talep_dict["tamamlanma_tarihi"] = t.tamamlanma_tarihi.isoformat() if t.tamamlanma_tarihi else None
#         talep_dict["silinme_tarihi"] = t.silinme_tarihi.isoformat() if t.silinme_tarihi else None
#         
#         # 5. Tutar hesaplama (30. Madde çözümü korunarak)
#         mevcut_tutar = float(t.tahmini_tutar) if t.tahmini_tutar else 0.0
#         if mevcut_tutar == 0.0 and hizmet and hizmet.varsayilan_fiyat:
#             talep_dict["tahmini_tutar"] = float(hizmet.varsayilan_fiyat)
#         else:
#             talep_dict["tahmini_tutar"] = mevcut_tutar
#             
#         sonuc.append(talep_dict)
#         
#     return sonuc

@app.get("/admin/servis-talepleri/gecmis")
def admin_gecmis_talepleri_getir(
    skip: int = Query(0, ge=0),
    limit: int = Query(20, ge=1, le=100),
    durum: Optional[str] = Query(None),
    arama: Optional[str] = Query(None),
    db: Session = Depends(get_db)
):
    query = db.query(models.ServisTalebi).filter(
        models.ServisTalebi.durum.in_(['Tamamlandı', 'İptal Edildi'])
    )

    if durum and durum != "Tümü":
        query = query.filter(models.ServisTalebi.durum == durum)

    if arama:
        arama = arama.lower()
        # Join'leri düzgün şekilde ekleyelim
        query = query.join(models.Kullanici, models.ServisTalebi.kullanici_id == models.Kullanici.id)\
                     .join(models.Arac, models.ServisTalebi.arac_id == models.Arac.id)\
                     .join(models.Hizmet, models.ServisTalebi.hizmet_id == models.Hizmet.id)\
                     .outerjoin(models.Marka, models.Arac.marka_id == models.Marka.id)\
                     .outerjoin(models.Model, models.Arac.model_id == models.Model.id)

        query = query.filter(
            (models.Kullanici.ad_soyad.ilike(f"%{arama}%")) |
            (models.Arac.ozel_marka.ilike(f"%{arama}%")) |
            (models.Arac.ozel_model.ilike(f"%{arama}%")) |
            (models.Marka.ad + " " + models.Model.ad).ilike(f"%{arama}%") |
            (models.Hizmet.ad.ilike(f"%{arama}%"))
        )

    toplam_kayit = query.count()

    talepler = query.order_by(models.ServisTalebi.talep_tarihi.desc())\
                    .offset(skip).limit(limit).all()

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
                arac_adi = f"{arac.ozel_marka} {arac.ozel_model}" if arac.ozel_marka else f"Araç ID: {arac.id}"

        talep_dict = {c.name: getattr(t, c.name) for c in t.__table__.columns}
        talep_dict["kullanici_ad_soyad"] = kullanici.ad_soyad if kullanici else "Bilinmiyor"
        talep_dict["arac_adi_tam"] = arac_adi
        talep_dict["hizmet_adi"] = hizmet.ad if hizmet else ""

        if t.talep_tarihi:
            talep_dict["talep_tarihi"] = t.talep_tarihi.strftime("%Y-%m-%d %H:%M")
        if t.tamamlanma_tarihi:
            talep_dict["tamamlanma_tarihi"] = t.tamamlanma_tarihi.isoformat()
        if t.silinme_tarihi:
            talep_dict["silinme_tarihi"] = t.silinme_tarihi.isoformat()

        iptal_eden = None
        if t.iptal_eden_id:
            iptal_eden = db.query(models.Kullanici).filter(models.Kullanici.id == t.iptal_eden_id).first()
        talep_dict["iptal_eden_ad_soyad"] = iptal_eden.ad_soyad if iptal_eden else None

        sonuc.append(talep_dict)

    return {
        "talepler": sonuc,
        "toplam_kayit": toplam_kayit
    }


# --- ADMİN: TALEP GÜNCELLEME (UYARI SİLİCİ) --- 
# from pydantic import BaseModel en üstte tanımladım.

# ---------------------------------------------------------
# ADMİN BİR TALEBİ GÜNCELLEDİĞİ VEYA İPTAL ETTİĞİNDE ÇALIŞAN METOT
# ---------------------------------------------------------
@app.put("/admin/servis-talepleri/{talep_id}/guncelle")
def admin_talep_guncelle(talep_id: int, istek: schemas.TalepAdminGuncelle, db: Session = Depends(get_db)):
    talep = db.query(models.ServisTalebi).filter(models.ServisTalebi.id == talep_id).first()
    if not talep:
        raise HTTPException(status_code=404, detail="Talep bulunamadı")
    
    eski_durum = talep.durum
    # eski_tutar = talep.tahmini_tutar BURADA NİYE TANMLANMIŞ VE KULLANILMAMIŞ ANLAMADIM.
    
    zaman_simdi = datetime.now()
    talep.durum = istek.yeni_durum
    talep.tahmini_tutar = istek.tahmini_tutar
    talep.guncelleme_tarihi = zaman_simdi # Her güncellemede bu tarih güncellenmeli
        
    # --- MADDE 40 REVİZESİ: EĞER DURUM TAMAMLANDI YAPILDIYSA TARİH AT ---
    # EĞER ADMİN İPTAL ETTİYSE:
    # --- ÇAĞATAY ABİ'NİN KURALLARI ---
    
    if istek.yeni_durum == "Tamamlandı":
        talep.kayit_durumu = 'A'
        talep.silinme_tarihi = None
        talep.tamamlanma_tarihi = zaman_simdi
        talep.iptal_eden_id = None
        
    elif istek.yeni_durum == "İptal Edildi":
        talep.kayit_durumu = 'X'
        talep.silinme_tarihi = zaman_simdi
        talep.tamamlanma_tarihi = None
        
        # İptal eden ID ataması
        if istek.islem_yapan_id is not None and istek.islem_yapan_id > 0:
            talep.iptal_eden_id = istek.islem_yapan_id
        else:
            ilk_admin = db.query(models.Kullanici).filter(models.Kullanici.rol == "Admin").first()
            talep.iptal_eden_id = ilk_admin.id if ilk_admin else None
            
    else:
        # Diğer durumlar (Onaylandı, İşlemde, Bekliyor) için tarihsel alanları koru
        talep.kayit_durumu = 'A'
        talep.silinme_tarihi = None
        talep.tamamlanma_tarihi = None
        talep.iptal_eden_id = None
    # -----------------------------------
        
    # ADMİN MÜDAHALE ETTİĞİNDE VEYA DURUMU DEĞİŞTİRDİĞİNDE UYARI BAYRAĞINI TEMİZLİYORUZ
    if talep.duzeltme_istendi_mi:
        talep.duzeltme_istendi_mi = False
        talep.duzeltme_notu = None
            
    log_kaydet(
        db=db, 
        islem="Admin Talep Güncellemesi", 
        detay=f"Talep ID: {talep_id} güncellendi. Güncelleyen kullanıcı ID: {talep.iptal_eden_id}. Durum: {eski_durum}->{istek.yeni_durum}", 
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
                    title="Oto Servis Bakım",
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
def admin_talep_guncelle_kullanilmiyor(talep_id: int, durum: str, tahmini_tutar: float, db: Session = Depends(get_db)):
    talep = db.query(models.ServisTalebi).filter(models.ServisTalebi.id == talep_id).first()
    if not talep:
        raise HTTPException(status_code=404, detail="Talep bulunamadı")
    
    eski_durum = talep.durum
    talep.durum = durum
    talep.tahmini_tutar = tahmini_tutar
    
    # --- MADDE 40 REVİZESİ: EĞER DURUM TAMAMLANDI YAPILDIYSA TARİH AT ---
    # ESKİ KOD (Yazım hatası içeriyordu): if talep.yeni_durum == "Tamamlandı" and eski_durum != "Tamamlandı":
    # YENİ REVİZE: talep.yeni_durum yerine metoda parametre gelen 'durum' değişkenini kullanıyoruz.
    if durum == "Tamamlandı" and eski_durum != "Tamamlandı":
        talep.tamamlanma_tarihi = datetime.now()
    
    if durum.yeni_durum == "İptal Edildi" and eski_durum != "İptal Edildi":
        talep.iptal_eden_id = durum.islem_yapan_id # Hangi admin sildiyse onu kaydet
    # --------------------------------------------------------------------
    
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

# Admin İçin Sayfalı Endpoint - DeepSeek
# ------------------------------------------------------------
# ADMİN TALEPLERİ (SAYFALI + FİLTRELİ)
# ------------------------------------------------------------
@app.get("/admin/servis-talepleri/sayfali")
def admin_taleplerini_sayfali_getir(
    skip: int = Query(0, ge=0),
    limit: int = Query(20, ge=1, le=100),
    durum: Optional[str] = Query(None),
    arama: Optional[str] = Query(None),
    db: Session = Depends(get_db)
):
    query = db.query(models.ServisTalebi).filter(
        models.ServisTalebi.kayit_durumu == 'A',
        models.ServisTalebi.durum.in_(['Bekliyor', 'Onaylandı', 'İşlemde'])
    )

    if durum and durum != "Tümü":
        query = query.filter(models.ServisTalebi.durum == durum)

    if arama:
        arama = arama.lower()
        query = query.join(models.Kullanici, models.ServisTalebi.kullanici_id == models.Kullanici.id)\
                     .join(models.Arac, models.ServisTalebi.arac_id == models.Arac.id)
        # Araç marka/model join'leri
        query = query.outerjoin(models.Marka, models.Arac.marka_id == models.Marka.id)\
                     .outerjoin(models.Model, models.Arac.model_id == models.Model.id)

        query = query.filter(
            (models.Kullanici.ad_soyad.ilike(f"%{arama}%")) |
            (models.Arac.ozel_marka.ilike(f"%{arama}%")) |
            (models.Arac.ozel_model.ilike(f"%{arama}%")) |
            (models.Marka.ad + " " + models.Model.ad).ilike(f"%{arama}%")
        )

    toplam_kayit = query.count()

    siralama = case(
        (models.ServisTalebi.durum == 'Bekliyor', 1),
        (models.ServisTalebi.durum == 'Onaylandı', 2),
        (models.ServisTalebi.durum == 'İşlemde', 3),
        else_=4
    )
    talepler = query.order_by(siralama, models.ServisTalebi.talep_tarihi.asc(), models.ServisTalebi.insert_tarihi.asc())\
                    .offset(skip).limit(limit).all()

    # İlişkili verileri ekleyelim (C# tarafı tekrar tekrar çekmesin)
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
                arac_adi = f"{arac.ozel_marka} {arac.ozel_model}" if arac.ozel_marka else f"Araç ID: {arac.id}"

        talep_dict = {c.name: getattr(t, c.name) for c in t.__table__.columns}
        talep_dict["kullanici_ad_soyad"] = kullanici.ad_soyad if kullanici else "Bilinmiyor"
        talep_dict["kullanici_telefon"] = kullanici.telefon if kullanici else "Belirtilmemiş"
        talep_dict["arac_adi_tam"] = arac_adi
        talep_dict["hizmet_adi"] = hizmet.ad if hizmet else ""

        if t.talep_tarihi:
            talep_dict["talep_tarihi"] = t.talep_tarihi.strftime("%Y-%m-%d %H:%M")

        sonuc.append(talep_dict)

    return {
        "talepler": sonuc,
        "toplam_kayit": toplam_kayit
    }

#################################################################
######################### ADMİN PANELİ ##########################
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

@app.delete("/bildirimler/{bildirim_id}")
def bildirim_sil(bildirim_id: int, db: Session = Depends(get_db)):
    bildirim = db.query(models.SistemBildirimleri).filter(models.SistemBildirimleri.id == bildirim_id).first()
    
    if not bildirim:
        raise HTTPException(status_code=404, detail="Silinmek istenen bildirim bulunamadı.")
    
    # Bildirimleri X durumuna çekmiyoruz, veritabanını şişirmemek için direkt siliyoruz (Madde 50)
    db.delete(bildirim)
    db.commit()
    
    return {"mesaj": "Bildirim başarıyla silindi."}

# DeepSeek Bildirimleri sayfalı getirme Lazy Loading - /bildirimler/{kullanici_id} endpointi artık kullanılmayabilir.
@app.get("/bildirimler/{kullanici_id}/sayfali")
def bildirimleri_sayfali_getir(
    kullanici_id: int,
    skip: int = Query(0, ge=0),
    limit: int = Query(20, ge=1, le=100),
    db: Session = Depends(get_db)
):
    query = db.query(models.SistemBildirimleri).filter(
        models.SistemBildirimleri.kullanici_id == kullanici_id
    )
    toplam_kayit = query.count()

    bildirimler = query.order_by(models.SistemBildirimleri.olusturulma_tarihi.desc())\
                       .offset(skip).limit(limit).all()

    return {
        "bildirimler": bildirimler,
        "toplam_kayit": toplam_kayit
    }


@app.post("/kullanici/token-kaydet/")
def token_kaydet(istek: schemas.TokenKayitIstegi, db: Session = Depends(get_db)):
    try:
        kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == istek.kullanici_id).first()
        if not kullanici:
            raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı")
        
        # --- YENİ REVİZE: (Madde 44) Token Optimizasyonu ---
        # Eğer gelen token veritabanındakiyle aynıysa db'yi yormadan çık.
        if kullanici.fcm_token == istek.fcm_token:
            print(f"Token zaten güncel (Değişiklik yok) -> Kullanıcı ID: {istek.kullanici_id}")
            return {"basari": True, "mesaj": "Token zaten güncel, kayıt atlandı"}
        # ---------------------------------------------------

        # Eğer token farklıysa veya ilk defa ekleniyorsa kaydet:        
        kullanici.fcm_token = istek.fcm_token
        db.commit()
        
        print(f"Token güncellendi -> Kullanıcı ID: {istek.kullanici_id}")
        return {"basari": True, "mesaj": "FCM Token başarıyla kaydedildi"}
        
    except Exception as e:
        db.rollback()
        print(f"Token kayıt hatası: {str(e)}")
        raise HTTPException(status_code=500, detail="Token kaydedilemedi")

# ==========================================
# --- MADDE 33: ADMİN FİYAT YÖNETİMİ ---
# ==========================================

# --- ESKİ KOD BAŞLANGICI (Yoruma Alındı) ---
# @app.put("/admin/hizmetler/{hizmet_id}/fiyat")
# def admin_hizmet_fiyat_guncelle(hizmet_id: int, istek: dict, db: Session = Depends(get_db)):
#     hizmet = db.query(models.Hizmet).filter(models.Hizmet.id == hizmet_id).first()
#     if not hizmet:
#         raise HTTPException(status_code=404, detail="Hizmet bulunamadı")
#     yeni_fiyat = istek.get("yeni_fiyat")
#     if yeni_fiyat is None:
#         raise HTTPException(status_code=400, detail="Yeni fiyat belirtilmedi")
#     hizmet.onceki_fiyat = hizmet.varsayilan_fiyat
#     hizmet.varsayilan_fiyat = yeni_fiyat 
#     db.commit()
#     return {"mesaj": "Fiyat başarıyla güncellendi", "yeni_fiyat": hizmet.varsayilan_fiyat}
# --- ESKİ KOD BİTİŞİ ---

# --- YENİ REVİZE BAŞLANGICI (Madde 33 - Arşiv & Log Destekli) ---
@app.put("/admin/hizmetler/{hizmet_id}/fiyat")
def admin_hizmet_fiyat_guncelle(hizmet_id: int, istek: dict, db: Session = Depends(get_db)):
    hizmet = db.query(models.Hizmet).filter(models.Hizmet.id == hizmet_id).first()
    if not hizmet:
        raise HTTPException(status_code=404, detail="Hizmet bulunamadı")
    
    yeni_fiyat = istek.get("yeni_fiyat")
    if yeni_fiyat is None:
        raise HTTPException(status_code=400, detail="Yeni fiyat belirtilmedi")
        
    eski_fiyat = hizmet.varsayilan_fiyat

    # 1. Ana tablodaki fiyatı güncelle
    hizmet.onceki_fiyat = eski_fiyat
    hizmet.varsayilan_fiyat = yeni_fiyat 
    
    # 2. Arşiv (Hizmet Fiyat Geçmişi) tablosuna kaydı oluştur
    fiyat_gecmisi_kayit = models.HizmetFiyatGecmisi(
        hizmet_id=hizmet.id,
        eski_fiyat=eski_fiyat,
        yeni_fiyat=yeni_fiyat
    )
    db.add(fiyat_gecmisi_kayit)

    # 3. Sistem Logları tablosuna INFO seviyesinde yapılandır
    # Not: Tarihi metne gömmüyoruz, veritabanındaki insert_tarihi kolonu bunu otomatik hallediyor.
    log_detay = f"'{hizmet.ad}' adlı hizmet {eski_fiyat} ₺ fiyatından {yeni_fiyat} ₺ fiyatına güncellenmiştir."
    
    sistem_log = models.SistemLog(
        kullanici_id=1,
        seviye="INFO",
        islem="Fiyat Güncellemesi",
        detay=log_detay
    )
    db.add(sistem_log)

    # 4. Tüm insert ve update işlemlerini tek seferde onayla
    db.commit()
    
    return {"mesaj": "Fiyat başarıyla güncellendi ve arşive işlendi", "yeni_fiyat": hizmet.varsayilan_fiyat}
# --- YENİ REVİZE BİTİŞİ ---


# ==========================================
# --- MADDE 34: ADMİN KULLANICI YÖNETİMİ ---
# ==========================================

# --- ESKİ KOD BAŞLANGICI (Yoruma Alındı) ---
# @app.get("/admin/kullanicilar")
# def admin_kullanicilari_getir(db: Session = Depends(get_db)):
#     kullanicilar = db.query(models.Kullanici).all()
#     return kullanicilar
# --- ESKİ KOD BİTİŞİ ---

# --- YENİ REVİZE BAŞLANGICI (Güvenli JSON Serileştirme Eklendi) ---
@app.get("/admin/kullanicilar")
def admin_kullanicilari_getir(
    sayfa: int = 1,
    sayfa_boyutu: int = 10,
    arama: str = "",
    db: Session = Depends(get_db)
):
    # Sayfalama hesaplaması (offset)                         
    atla = (sayfa - 1) * sayfa_boyutu
    
    # ESKİ KOD: query = db.query(models.Kullanici).filter(models.Kullanici.kayit_durumu == 'A')
    # YENİ REVİZE: (Tüm kullanıcıları getirmesi için filtre kaldırıldı, zaten admin hepsini görmeli)
    query = db.query(models.Kullanici)
    
    # Eğer arama kelimesi varsa filtrele                                 
    if arama:
        query = query.filter(
            (models.Kullanici.ad_soyad.ilike(f"%{arama}%")) |
            (models.Kullanici.eposta.ilike(f"%{arama}%"))
        )
        
    toplam_kayit = query.count()                
    kullanicilar = query.offset(atla).limit(sayfa_boyutu).all()

    # Güvenli serileştirme (sadece ihtiyaç duyulan alanlar)                                       
    guvenli_liste = []
    for k in kullanicilar:
        guvenli_liste.append({
            "id": k.id,
            "ad_soyad": k.ad_soyad,
            "eposta": k.eposta,
            "telefon": k.telefon,
            "rol": k.rol,
            "aktif_mi": k.aktif_mi,
            # Tarihleri string formatında güvenle C# tarafına yolluyoruz
            "kayit_tarihi": k.kayit_tarihi.strftime("%d.%m.%Y %H:%M") if hasattr(k, 'kayit_tarihi') and k.kayit_tarihi else "-",
            "silinme_tarihi": k.silinme_tarihi.strftime("%d.%m.%Y %H:%M") if hasattr(k, 'silinme_tarihi') and k.silinme_tarihi else "-"
        })
    #########################################
    # geçici olarak eklendi hangi sorgunun çalıştığını ve kaç satır veri döndüğünü görmek için.   
    # import logging
    # logging.basicConfig()
    # logging.getLogger('sqlalchemy.engine').setLevel(logging.INFO)
    #########################################
    return {
        "kullanicilar": guvenli_liste,
        "toplam_kayit": toplam_kayit,
                                
        "toplam_sayfa": (toplam_kayit + sayfa_boyutu - 1) // sayfa_boyutu,
        "gecerli_sayfa": sayfa
    }
# --- YENİ REVİZE BİTİŞİ ---

@app.put("/admin/kullanicilar/{kullanici_id}/durum")
def admin_kullanici_durum_guncelle(kullanici_id: int, istek: dict, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı")
        
    aktif_mi = istek.get("aktif_mi")
    if aktif_mi is None:
        raise HTTPException(status_code=400, detail="Durum belirtilmedi")
        
    kullanici.aktif_mi = aktif_mi 
    db.commit()
    
    durum_metni = "Aktifleştirildi" if aktif_mi else "Pasife Alındı"
    return {"mesaj": f"Kullanıcı durumu güncellendi: {durum_metni}"}


@app.put("/admin/kullanicilar/{kullanici_id}/guncelle")
def admin_kullanici_guncelle(kullanici_id: int, istek: dict, db: Session = Depends(get_db)):
    kullanici = db.query(models.Kullanici).filter(models.Kullanici.id == kullanici_id).first()
    if not kullanici:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı")
        
    if "ad_soyad" in istek:
        kullanici.ad_soyad = istek["ad_soyad"]
        
    if "aktif_mi" in istek:
        kullanici.aktif_mi = istek["aktif_mi"]
        # Soft Delete: Pasife alınırsa silinme tarihi atar, aktife alınırsa tarihi temizler
        if hasattr(kullanici, 'silinme_tarihi'):
            if not kullanici.aktif_mi:
                kullanici.silinme_tarihi = datetime.now()
            else:
                kullanici.silinme_tarihi = None

    db.commit()
    return {"mesaj": "Kullanıcı başarıyla güncellendi"}


# main.py içerisine Dashboard verileri için yeni bir endpoint yapılandırıldı.
@app.get("/admin/dashboard-istatistik")
def dashboard_istatistik(db: Session = Depends(get_db)):
    # İleride tamamen otomatiğe geçmek istediğinde bu değişkenleri 0 yapman yeterli.
    manuel_musteri_ek = 130  
    manuel_talep_ek = 316 
    manuel_arac_ek = 139     

    # Veritabanından gelen otomatik sayımlar (Sadece aktifleri sayıyoruz)
    oto_musteri = db.query(models.Kullanici).filter(
        models.Kullanici.rol == "Musteri", 
        models.Kullanici.aktif_mi == True
    ).count()
    
    oto_talep = db.query(models.ServisTalebi).count()
    oto_arac = db.query(models.Arac).count()

    return {
        "toplam_musteri": oto_musteri + manuel_musteri_ek,
        "toplam_talep": oto_talep + manuel_talep_ek,
        "toplam_arac": oto_arac + manuel_arac_ek
    }
    
    
# ----------------- VİTRİN YÖNETİMİ -----------------
@app.get("/vitrin", response_model=List[schemas.TamamlananIsResponse])
def vitrin_listesi(db: Session = Depends(get_db)):
    return db.query(models.TamamlananIs).order_by(models.TamamlananIs.id.desc()).all()

@app.post("/admin/vitrin", response_model=schemas.TamamlananIsResponse)
async def vitrin_ekle(
    baslik: str = Form(...),
    aciklama: str = Form(...),
    etiket: str = Form(...),
    tarih: str = Form(...),
    hizmet_id: Optional[int] = Form(None),
    file: UploadFile = File(...),
    db: Session = Depends(get_db)
):
    try:
        # Klasör kontrolü
        os.makedirs("VitrinImg", exist_ok=True)

        # Dosya adı oluştur
        zaman_damgasi = datetime.now().strftime("%Y_%m_%d_%H%M_%S_%f")[:-3]
        dosya_uzanti = os.path.splitext(file.filename)[1]
        if not dosya_uzanti:
            dosya_uzanti = ".jpg"
        dosya_adi = f"vitrin_{zaman_damgasi}{dosya_uzanti}"
        dosya_yolu = os.path.join("VitrinImg", dosya_adi)

        # Resmi işle ve kaydet
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))
        
        image = ImageOps.exif_transpose(image) # fotoğrafın EXIF verisindeki yönlendirmeyi okuyup görseli otomatik olarak doğru pozisyona döndürür
        
        if image.mode in ("RGBA", "P"):
            image = image.convert("RGB")
        image.thumbnail((1024, 1024))
        image.save(dosya_yolu, "JPEG", quality=75)

        # Veritabanına kaydet
        resim_url = f"/VitrinImg/{dosya_adi}"
        yeni_is = models.TamamlananIs(
            baslik=baslik,
            aciklama=aciklama,
            etiket=etiket,
            tarih=tarih,
            resim_url=resim_url,
            hizmet_id=hizmet_id
        )
        db.add(yeni_is)
        db.commit()
        db.refresh(yeni_is)

        log_kaydet(db, "Vitrin Ekleme", f"'{baslik}' vitrine eklendi.", "INFO")
        return yeni_is

    except Exception as e:
        db.rollback()
        # Hata detayını logla
        print(f"Vitrin ekleme hatası: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Fotoğraf kaydedilirken hata oluştu: {str(e)}")

@app.put("/admin/vitrin/{is_id}", response_model=schemas.TamamlananIsResponse)
async def vitrin_guncelle(
    is_id: int,
    baslik: str = Form(...),
    aciklama: str = Form(...),
    etiket: str = Form(...),
    tarih: str = Form(...),
    hizmet_id: Optional[int] = Form(None),
    file: Optional[UploadFile] = File(None),
    db: Session = Depends(get_db)
):
    try:
        vitrin_is = db.query(models.TamamlananIs).filter(models.TamamlananIs.id == is_id).first()
        if not vitrin_is:
            raise HTTPException(status_code=404, detail="Vitrin öğesi bulunamadı")

        vitrin_is.baslik = baslik
        vitrin_is.aciklama = aciklama
        vitrin_is.etiket = etiket
        vitrin_is.tarih = tarih
        vitrin_is.hizmet_id = hizmet_id

        if file and file.filename:
            # Eski dosyayı sil
            if vitrin_is.resim_url.startswith("/VitrinImg/"):
                eski_dosya = os.path.join("VitrinImg", os.path.basename(vitrin_is.resim_url))
                if os.path.exists(eski_dosya):
                    os.remove(eski_dosya)

            # Yeni dosyayı kaydet
            zaman_damgasi = datetime.now().strftime("%Y_%m_%d_%H%M_%S_%f")[:-3]
            dosya_uzanti = os.path.splitext(file.filename)[1] or ".jpg"
            dosya_adi = f"vitrin_{zaman_damgasi}{dosya_uzanti}"
            dosya_yolu = os.path.join("VitrinImg", dosya_adi)
            contents = await file.read()
            image = Image.open(io.BytesIO(contents))
            
            image = ImageOps.exif_transpose(image) # fotoğrafın EXIF verisindeki yönlendirmeyi okuyup görseli otomatik olarak doğru pozisyona döndürür
            
            if image.mode in ("RGBA", "P"):
                image = image.convert("RGB")
            image.thumbnail((1024, 1024))
            image.save(dosya_yolu, "JPEG", quality=75)

            vitrin_is.resim_url = f"/VitrinImg/{dosya_adi}"

        db.commit()
        db.refresh(vitrin_is)
        log_kaydet(db, "Vitrin Güncelleme", f"'{baslik}' güncellendi.", "INFO")
        return vitrin_is

    except Exception as e:
        db.rollback()
        print(f"Vitrin güncelleme hatası: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Güncelleme sırasında hata oluştu: {str(e)}")

@app.delete("/admin/vitrin/{is_id}")
def vitrin_sil(is_id: int, db: Session = Depends(get_db)):
    vitrin_is = db.query(models.TamamlananIs).filter(models.TamamlananIs.id == is_id).first()
    if not vitrin_is:
        raise HTTPException(status_code=404, detail="Vitrin öğesi bulunamadı")

    if vitrin_is.resim_url.startswith("/VitrinImg/"):
        dosya_yolu = os.path.join("VitrinImg", os.path.basename(vitrin_is.resim_url))
        if os.path.exists(dosya_yolu):
            os.remove(dosya_yolu)

    db.delete(vitrin_is)
    db.commit()
    log_kaydet(db, "Vitrin Silme", f"'{vitrin_is.baslik}' vitrinden kaldırıldı.", "WARNING")
    return {"mesaj": "Vitrin öğesi silindi"}
#################################################################



#################################################################
######################### ADMİN PANELİ ##########################
#################################################################

# Backend – İstemci hatalarını loglayan endpoint
class ClientErrorLog(BaseModel):
    message: str
    stack_trace: Optional[str] = None
    source: Optional[str] = None   # hangi sayfa/sınıf

@app.post("/api/log-client-error")
def log_client_error(error: ClientErrorLog, db: Session = Depends(get_db)):
    try:
        # Kullanıcı ID’si yoksa -1 veya null bırakabiliriz
        yeni_log = models.SistemLog(
            seviye="ERROR",
            islem=f"Client: {error.source or 'Unknown'}",
            detay=f"{error.message}\n{error.stack_trace or ''}"
        )
        db.add(yeni_log)
        db.commit()
        return {"success": True}
    except Exception as e:
        print("Log yazma hatası:", e)
        return {"success": False}
    

#################################################################
##################### HASARLI RESİM EKLEME ######################
#################################################################
MaksimumFotoSayisi = 4
@app.post("/servis-talepleri/{talep_id}/fotograf")
async def fotograf_yukle(talep_id: int, db: Session = Depends(get_db), file: UploadFile = File(...)):
    talep = db.query(models.ServisTalebi).filter(models.ServisTalebi.id == talep_id).first()
    if not talep:
        raise HTTPException(status_code=404, detail="Talep bulunamadı.")

    mevcut_sayi = db.query(models.ServisTalebiFotograf).filter(models.ServisTalebiFotograf.talep_id == talep_id).count()
    if mevcut_sayi >= MaksimumFotoSayisi:
        raise HTTPException(status_code=400, detail=f"Bu talep için maksimum fotoğraf sınırına ({MaksimumFotoSayisi}) ulaşıldı.")

    try:
        # 1. Dosyayı belleğe al ve Pillow ile aç
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))
        
        # 2. Şeffaflık (PNG) varsa arka planı beyaza çevirip JPEG'e hazırla
        if image.mode in ("RGBA", "P"):
            image = image.convert("RGB")

        # 3. Boyutu küçült (Optimizasyon - max 1024x1024)
        image.thumbnail((1024, 1024))
        
        # 4. Benzersiz isimle kaydet
        # dosya_adi = f"{uuid.uuid4().hex}.jpg"
        # YENİ REVİZE: C# tarafından gönderilen özel ismi kullanıyoruz
        dosya_adi = file.filename        
        # Güvenlik: Eğer uzantı yoksa otomatik .jpg ekle
        if not dosya_adi.lower().endswith(('.png', '.jpg', '.jpeg')):
            dosya_adi += ".jpg"
        dosya_yolu = os.path.join("HasarImg", dosya_adi)
        image.save(dosya_yolu, "JPEG", quality=75) # Kalite %75 ile sıkıştırma

        # 5. DB'ye yaz
        yeni_foto = models.ServisTalebiFotograf(talep_id=talep_id, dosya_yolu=dosya_yolu)
        db.add(yeni_foto)
        db.commit()

        return {"mesaj": "Fotoğraf başarıyla yüklendi", "dosya_yolu": dosya_yolu}
    
    except Exception as e:
        db.rollback()
        raise HTTPException(status_code=500, detail=f"Fotoğraf işlenirken veya kaydedilirken sunucu hatası oluştu: {str(e)}")
    
# --- FOTOĞRAFLARI KOMPLE TEMİZLEME (ÜZERİNE YAZMA MANTIĞI İÇİN) ---
@app.delete("/servis-talepleri/{talep_id}/fotograflari-temizle")
def fotograflari_temizle(talep_id: int, db: Session = Depends(get_db)):
    fotolar = db.query(models.ServisTalebiFotograf).filter(models.ServisTalebiFotograf.talep_id == talep_id).all()
    
    for foto in fotolar:
        try:
            # Fiziksel dosyayı sunucudan sil
            if os.path.exists(foto.dosya_yolu):
                os.remove(foto.dosya_yolu)
        except:
            pass # Dosya bulunamazsa takılma, devam et
        
        # Veritabanından sil
        db.delete(foto)
        
    db.commit()
    return {"mesaj": "Eski fotoğraflar başarıyla temizlendi."}


# --- TALEBE AİT FOTOĞRAFLARI GETİRME ---
@app.get("/servis-talepleri/{talep_id}/fotograflar")
def get_fotograflar(talep_id: int, db: Session = Depends(get_db)):
    return db.query(models.ServisTalebiFotograf).filter(models.ServisTalebiFotograf.talep_id == talep_id).all()

# --- YENİ REVİZE: TEK BİR FOTOĞRAFI FİZİKSEL VE DB'DEN SİLME ---
@app.delete("/fotograflar/{foto_id}")
def fotograf_sil(foto_id: int, db: Session = Depends(get_db)):
    foto = db.query(models.ServisTalebiFotograf).filter(models.ServisTalebiFotograf.id == foto_id).first()
    if not foto:
        raise HTTPException(status_code=404, detail="Fotoğraf bulunamadı")
    
    # Dosyayı sunucudan fiziksel olarak uçuruyoruz
    try:
        if os.path.exists(foto.dosya_yolu):
            os.remove(foto.dosya_yolu)
    except:
        pass # Dosya diskte bulunamazsa bile veritabanından silmek için devam et
        
    db.delete(foto)
    db.commit()
    return {"mesaj": "Fotoğraf başarıyla silindi"}


class TopluFotografDurumuIstek(BaseModel):
    talep_idleri: List[int]

@app.post("/servis-talepleri/toplu-fotograf-durumu")
def toplu_fotograf_durumu(
    istek: TopluFotografDurumuIstek,
    db: Session = Depends(get_db)
) -> Dict[int, bool]:
    """
    Gönderilen talep ID'leri için fotoğraf var mı bilgisini döner.
    Yanıt: { talep_id: bool, ... }
    """
    sonuc = {}
    for talep_id in istek.talep_idleri:
        # Veritabanında o talebe ait en az bir fotoğraf kaydı var mı kontrol et
        var_mi = db.query(models.ServisTalebiFotograf).filter(
            models.ServisTalebiFotograf.talep_id == talep_id
        ).first() is not None
        sonuc[talep_id] = var_mi
    return sonuc


# Talep 85: Eski Hasar Fotoğraflarını Temizleme
ESKI_FOTOGRAF_GUN_SINIRI = 365  # 12 ay (365 gün)

async def eski_fotograflari_temizle_gorevi():
    """Periyodik olarak eski hasar ve vitrin fotoğraflarını temizler."""
    while True:
        try:
            db = SessionLocal()
            sinir_tarihi = datetime.now() - timedelta(days=ESKI_FOTOGRAF_GUN_SINIRI)
            
            # 1. Eski servis talebi fotoğraflarını bul
            eski_hasar_fotolar = db.query(models.ServisTalebiFotograf).filter(
                models.ServisTalebiFotograf.olusturulma_tarihi < sinir_tarihi
            ).all()
            
            for foto in eski_hasar_fotolar:
                try:
                    if os.path.exists(foto.dosya_yolu):
                        os.remove(foto.dosya_yolu)
                    db.delete(foto)
                except Exception as e:
                    print(f"Hasar fotoğraf silinirken hata: {e}")
            
            # 2. Eski vitrin fotoğraflarını bul (TamamlananIs tablosundan)
            eski_vitrin_fotolari = db.query(models.TamamlananIs).filter(
                models.TamamlananIs.olusturulma_tarihi < sinir_tarihi
            ).all()
            
            # for vitrin in eski_vitrin_fotolari:
            #     try:
            #         # Vitrin kaydını sil (fotoğraf da silinecek)
            #         if vitrin.resim_url and vitrin.resim_url.startswith("/VitrinImg/"):
            #             dosya_yolu = os.path.join("VitrinImg", os.path.basename(vitrin.resim_url))
            #             if os.path.exists(dosya_yolu):
            #                 os.remove(dosya_yolu)
            #         db.delete(vitrin)
            #     except Exception as e:
            #         print(f"Vitrin fotoğrafı silinirken hata: {e}")
            
            db.commit()
            print(f"✅ Eski fotoğraf temizliği tamamlandı. {len(eski_hasar_fotolar)} hasar, {len(eski_vitrin_fotolari)} vitrin fotoğrafı silindi.")
            
        except Exception as e:
            print(f"❌ Eski fotoğraf temizleme hatası: {e}")
        finally:
            db.close()
        
        # 24 saatte bir çalıştır
        await asyncio.sleep(24 * 3600)
        
# Talep 86: Otomatik Hatırlatma Maili (6 Ay)

HATIRLATMA_ARALIGI_AY = 6
HATIRLATMA_SPAM_KORUMA_GUN = 30  # Aynı kullanıcıya en az 30 günde bir mail at

async def otomatik_hatirlatma_gorevi():
    """Periyodik olarak uzun süredir giriş yapmayan kullanıcılara hatırlatma maili gönderir."""
    while True:
        try:
            db = SessionLocal()
            simdi = datetime.now()
            alti_ay_once = simdi - timedelta(days=HATIRLATMA_ARALIGI_AY * 30)  # 180 gün
            spam_koruma_tarihi = simdi - timedelta(days=HATIRLATMA_SPAM_KORUMA_GUN)

            print(f"🔄 Otomatik hatırlatma kontrolü başladı. Şu an: {simdi}")
            print(f"   - 6 ay öncesi: {alti_ay_once}")
            print(f"   - Spam koruma tarihi: {spam_koruma_tarihi}")

            # Son girişi 6 aydan eski, mail izni olan, aktif müşteriler
            query = db.query(models.Kullanici).filter(
                models.Kullanici.rol == "Musteri",
                models.Kullanici.aktif_mi == True,
                models.Kullanici.mail_istiyor_mu == True,
                models.Kullanici.son_giris_tarihi < alti_ay_once
            )

            # Hatırlatma koşulu: son_hatirlatma_tarihi NULL VEYA spam_koruma_tarihi'nden eski
            query = query.filter(
                or_(
                    models.Kullanici.son_hatirlatma_tarihi == None,
                    models.Kullanici.son_hatirlatma_tarihi < spam_koruma_tarihi
                )
            )

            kullanicilar = query.all()
            print(f"   - Filtreye uyan kullanıcı sayısı: {len(kullanicilar)}")

            for kullanici in kullanicilar:
                try:
                    mail_icerigi = f"""
                    <html>
                        <body style="font-family: Arial, sans-serif; color: #333; line-height: 1.6;">
                            <p>Selamlar <b>{kullanici.ad_soyad}</b>,</p>
                            <p>Sizi uzun zamandır aramızda göremedik. Bir kahvemizi içmeye bekliyoruz. Araç bakımlarınız için sizlere en iyi hizmeti sunmaya devam ediyoruz.</p>
                            <p>Uygulamamıza giriş yaparak yeni kampanyalarımızı ve fırsatlarımızı görebilirsiniz.</p>
                            <br>
                            <p>Hayırlı günler dileriz,<br><b>Oto Servis Bakım Yönetimi</b></p>
                            <hr>
                            <p style="font-size: 11px; color: #999;">
                            Bu e-postayı almak istemiyorsanız <a href="http://136.115.53.49:8000/kvkk/mail-iptal/{kullanici.id}">buraya tıklayarak</a> abonelikten çıkabilirsiniz.
                            </p>
                        </body>
                    </html>
                    """
                    if eposta_gonder(kullanici.eposta, "Sizi Özledik! - Oto Servis Bakım", mail_icerigi):
                        kullanici.son_hatirlatma_tarihi = simdi
                        db.commit()
                        print(f"✅ Hatırlatma maili gönderildi: {kullanici.eposta}")
                        # ... Push bildirimi de gönder ...
                        if kullanici.fcm_token:
                            try:
                                mesaj = messaging.Message(
                                    notification=messaging.Notification(
                                        title="Sizi Özledik!",
                                        body="Uygulamamıza uzun zamandır girmediniz, sizi bekliyoruz!"
                                    ),
                                    token=kullanici.fcm_token
                                )
                                messaging.send(mesaj)
                            except Exception as e:
                                print(f"Push hatası: {e}")
                    else:
                        print(f"❌ Mail gönderilemedi: {kullanici.eposta}")
                except Exception as e:
                    print(f"Kullanıcı {kullanici.eposta} için hata: {e}")

            db.close()
            print(f"✅ Otomatik hatırlatma tamamlandı.")

        except Exception as e:
            print(f"❌ Otomatik hatırlatma hatası: {e}")

        await asyncio.sleep(24 * 3600)  # 24 saat bekle
        
