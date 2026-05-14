import os
from sqlalchemy import create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker
from dotenv import load_dotenv
from cryptography.fernet import Fernet

load_dotenv()

DB_USER = os.getenv("DB_USER")
DB_HOST = os.getenv("DB_HOST")
DB_NAME = os.getenv("DB_NAME")

# Şifreli metni ve çözücü anahtarı al
SIFRELENMIS_PASSWORD = os.getenv("DB_PASSWORD")
ENCRYPTION_KEY = os.getenv("ENCRYPTION_KEY")

# --- ŞİFRE ÇÖZME İŞLEMİ (MADDE 27 VİZYONU) ---
try:
    if ENCRYPTION_KEY and SIFRELENMIS_PASSWORD:
        cipher_suite = Fernet(ENCRYPTION_KEY.encode())
        # Karmaşık metni gerçek şifreye çevir (Sadece RAM üzerinde, dosyalarda görünmez!)
        DB_PASSWORD = cipher_suite.decrypt(SIFRELENMIS_PASSWORD.encode()).decode()
    else:
        # Eğer anahtar yoksa, güvenlik gereği sistemi durdur veya boş şifre ata
        DB_PASSWORD = ""
except Exception as e:
    print(f"KRİTİK GÜVENLİK HATASI: Veritabanı şifresi çözülemedi! Anahtarı kontrol edin. Hata: {e}")
    DB_PASSWORD = ""
# ---------------------------------------------

SQLALCHEMY_DATABASE_URL = f"mysql+pymysql://{DB_USER}:{DB_PASSWORD}@{DB_HOST}/{DB_NAME}"

#engine = create_engine(SQLALCHEMY_DATABASE_URL)
engine = create_engine(
    SQLALCHEMY_DATABASE_URL,
    pool_pre_ping=True,   # Her sorgudan önce bağlantıyı kontrol et, kopmuşsa yeniden bağlan
    pool_recycle=3600     # Bağlantıları her 1 saatte bir otomatik yenile (MySQL wait_timeout süresinden kısa olmalı)
)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()