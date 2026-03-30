from fastapi import FastAPI
import models
from database import engine

# Veritabanı tablolarını oluşturur (Eğer yoksa MySQL'de otomatik açar)
models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="Kapıdan Bakım API", version="1.0.0")

@app.get("/")
def read_root():
    return {"mesaj": "Kapıdan Bakım API Sistemine Hoş Geldiniz!", "durum": "Aktif"}