from fastapi import FastAPI, Depends, HTTPException
from sqlalchemy.orm import Session
import models, schemas
from database import engine, get_db

# Tabloları oluştur
models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="Kapıdan Bakım API", version="1.0.0")

@app.get("/")
def read_root():
    return {"mesaj": "Kapıdan Bakım API Sistemine Hoş Geldiniz!", "durum": "Aktif"}

# --- KULLANICI İŞLEMLERİ ---
@app.post("/users/", response_model=schemas.User)
def create_user(user: schemas.UserCreate, db: Session = Depends(get_db)):
    # Email kontrolü
    db_user = db.query(models.User).filter(models.User.email == user.email).first()
    if db_user:
        raise HTTPException(status_code=400, detail="Bu email adresi zaten kayıtlı.")
    
    # Şifreyi şimdilik düz kaydediyoruz, güvenlik aşamasında hash'leyeceğiz
    new_user = models.User(
        full_name=user.full_name,
        email=user.email,
        phone=user.phone,
        hashed_password=user.password 
    )
    db.add(new_user)
    db.commit()
    db.refresh(new_user)
    return new_user

# --- ARAÇ İŞLEMLERİ ---
@app.post("/vehicles/", response_model=schemas.Vehicle)
def create_vehicle(vehicle: schemas.VehicleCreate, db: Session = Depends(get_db)):
    # modeli dictionary'e çevirip (**kwargs) pratik bir şekilde tabloya basıyoruz
    new_vehicle = models.Vehicle(**vehicle.model_dump())
    db.add(new_vehicle)
    db.commit()
    db.refresh(new_vehicle)
    return new_vehicle

# --- SERVİS TALEBİ İŞLEMLERİ ---
@app.post("/service-requests/", response_model=schemas.ServiceRequest)
def create_service_request(request: schemas.ServiceRequestCreate, db: Session = Depends(get_db)):
    new_request = models.ServiceRequest(**request.model_dump())
    db.add(new_request)
    db.commit()
    db.refresh(new_request)
    return new_request

# --- KULLANICI DETAYI GETİRME (İçindeki araçlar ve taleplerle birlikte) ---
@app.get("/users/{user_id}", response_model=schemas.User)
def get_user(user_id: int, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.id == user_id).first()
    if user is None:
        raise HTTPException(status_code=404, detail="Kullanıcı bulunamadı")
    return user

# --- REFERANS VERİLERİ (AÇILIR LİSTELER İÇİN) ---

@app.get("/reference-data/brands/")
def get_brands():
    # Şimdilik en popüler markaları alfabetik sırayla döndürüyoruz
    return ["BMW", "Fiat", "Ford", "Mercedes", "Renault", "Volkswagen"]

@app.get("/reference-data/models/{brand}")
def get_models(brand: str):
    # Seçilen markaya göre modelleri döndüren dictionary yapısı
    models_data = {
        "BMW": ["1.16i", "3.20i", "5.20d", "X5"],
        "Fiat": ["Doblo", "Egea", "Fiorino", "Punto"],
        "Ford": ["Fiesta", "Focus", "Kuga", "Puma"],
        "Mercedes": ["A180", "C180", "E250", "GLA"],
        "Renault": ["Captur", "Clio", "Megane", "Taliant"],
        "Volkswagen": ["Golf", "Passat", "Polo", "Tiguan"]
    }
    # Eğer gelen marka listede yoksa boş liste döndür
    return models_data.get(brand, [])

@app.get("/reference-data/fuel-types/")
def get_fuel_types():
    return ["Benzin", "Dizel", "LPG", "Elektrik", "Hibrit"]