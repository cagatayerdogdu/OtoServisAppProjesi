from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, declarative_base

# MySQL bağlantı cümlemiz (kullanıcı adı: root, şifre: boş varsaydım, veritabanı: otoservisdb)
# Eğer XAMPP veya yerel MySQL'de şifren varsa 'root:sifren@localhost...' şeklinde güncellemelisin.
SQLALCHEMY_DATABASE_URL = "mysql+pymysql://root:Ccee3344!@localhost:3306/otoservisdb"

engine = create_engine(SQLALCHEMY_DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

Base = declarative_base()

# Veritabanı oturumu oluşturmak için dependency (bağımlılık) fonksiyonu
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()