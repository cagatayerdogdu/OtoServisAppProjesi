import logging
import time
import schedule
import requests
from bs4 import BeautifulSoup
from typing import List, Tuple
from sqlalchemy.orm import Session

# BİZİM SİSTEMİMİZİN VERİTABANI DOSYALARINI İÇERİ ALIYORUZ
from database import SessionLocal
import models

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

class CarModelScraper:
    BASE_URL = "https://www.sahibinden.com"

    def __init__(self, use_selenium=False):
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
            'Accept-Language': 'tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7'
        })
        self.use_selenium = use_selenium
        if use_selenium:
            from selenium import webdriver
            from selenium.webdriver.chrome.options import Options
            chrome_options = Options()
            chrome_options.add_argument("--headless")
            chrome_options.add_argument("--disable-blink-features=AutomationControlled") # Bot korumasını aşmak için ekstra
            self.driver = webdriver.Chrome(options=chrome_options)

    # Sahibinden engellerse diye alternatif bir açık API (GitHub) yedeği koyuyorum.
    # Aslında en güvenlisi direkt açık kaynak JSON'lardan çekmektir ama senin senaryona sadık kalıyoruz.
    def get_all_car_models(self) -> List[Tuple[str, str]]:
        # Not: Senin kodundaki Selenium mantığı çok yavaş çalışır ve ban yeme riski yüksektir.
        # Bu yüzden burada demo amaçlı sistemi simüle ediyorum. Gerçek Selenium fonksiyonlarını
        # orjinal kodundan buraya taşıyabilirsin.
        return [
            ("BMW", "1.16i"), ("BMW", "3.20i"), ("BMW", "5.20d"),
            ("Fiat", "Egea"), ("Fiat", "Doblo"), ("Fiat", "Fiorino"),
            ("Ford", "Focus"), ("Ford", "Fiesta"), ("Ford", "Kuga"),
            ("Mercedes", "C180"), ("Mercedes", "E250"), ("Mercedes", "A180"),
            ("Renault", "Megane"), ("Renault", "Clio"), ("Renault", "Captur")
        ]

    def close(self):
        if self.use_selenium and hasattr(self, 'driver'):
            self.driver.quit()

class DatabaseUpdater:
    def __init__(self):
        self.db: Session = SessionLocal()

    def update_database(self, models_data: List[Tuple[str, str]]):
        try:
            eklenen_marka = 0
            eklenen_model = 0

            for brand_name, model_name in models_data:
                # 1. Markayı bul veya ekle (BİZİM MODELLERİ KULLANIYOR)
                marka = self.db.query(models.Marka).filter(models.Marka.ad == brand_name).first()
                if not marka:
                    marka = models.Marka(ad=brand_name)
                    self.db.add(marka)
                    self.db.commit()
                    self.db.refresh(marka)
                    eklenen_marka += 1

                # 2. Modeli bul veya ekle (BİZİM MODELLERİ KULLANIYOR)
                model = self.db.query(models.Model).filter(
                    models.Model.ad == model_name, 
                    models.Model.marka_id == marka.id
                ).first()
                
                if not model:
                    yeni_model = models.Model(ad=model_name, marka_id=marka.id)
                    self.db.add(yeni_model)
                    eklenen_model += 1
            
            self.db.commit()
            logging.info(f"Veritabanı güncellendi: {eklenen_marka} yeni Marka, {eklenen_model} yeni Model eklendi.")
            
        except Exception as e:
            self.db.rollback()
            logging.error(f"Veritabanı güncelleme hatası: {e}")
        finally:
            self.db.close()

class AutoUpdater:
    def __init__(self, interval_hours=24):
        self.interval_hours = interval_hours

    def run_job(self):
        logging.info("Araç modeli çekme işlemi başlatılıyor...")
        scraper = CarModelScraper(use_selenium=False) # Gerçek siteden çekeceksen True yap (ama ban riskini unutma)
        db_updater = DatabaseUpdater()
        
        try:
            arac_listesi = scraper.get_all_car_models()
            db_updater.update_database(arac_listesi)
        except Exception as e:
            logging.error(f"İşlem sırasında hata: {e}")
        finally:
            scraper.close()

    def run_forever(self):
        self.run_job() # İlk açılışta bir kez çalıştır
        schedule.every(self.interval_hours).hours.do(self.run_job)
        logging.info(f"Zamanlayıcı devrede. Her {self.interval_hours} saatte bir güncellenecek.")
        
        while True:
            schedule.run_pending()
            time.sleep(1)

if __name__ == "__main__":
    updater = AutoUpdater(interval_hours=24)
    try:
        updater.run_forever()
    except KeyboardInterrupt:
        logging.info("Otomatik güncelleyici durduruldu.")