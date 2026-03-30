import logging
import time
import schedule
import requests
from bs4 import BeautifulSoup
from typing import List, Tuple
import json
import mysql.connector
from mysql.connector import Error
import logging

# ------------------------------------------------------------------
# Scraper (sahibinden.com için)
# ------------------------------------------------------------------
class CarModelScraper:
    BASE_URL = "https://www.sahibinden.com"

    def __init__(self, use_selenium=False):
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
        })
        self.use_selenium = use_selenium
        if use_selenium:
            from selenium import webdriver
            from selenium.webdriver.chrome.options import Options
            chrome_options = Options()
            chrome_options.add_argument("--headless")
            self.driver = webdriver.Chrome(options=chrome_options)

    def get_brands(self) -> List[str]:
        """Marka listesini al (dropdown'tan)."""
        if self.use_selenium:
            return self._get_brands_selenium()
        else:
            return self._get_brands_requests()

    def _get_brands_requests(self) -> List[str]:
        url = f"{self.BASE_URL}/otomobil"
        resp = self.session.get(url)
        soup = BeautifulSoup(resp.text, 'html.parser')
        # Sahibinden'de brand dropdown id'si genelde "brand" veya "marka"
        brand_select = soup.find('select', {'id': 'brand'}) or soup.find('select', {'name': 'brand'})
        if not brand_select:
            logging.warning("Marka dropdown'ı bulunamadı.")
            return []
        options = brand_select.find_all('option')
        brands = [opt.get_text(strip=True) for opt in options if opt.get('value') and opt['value'] != '']
        return brands

    def _get_brands_selenium(self) -> List[str]:
        self.driver.get(f"{self.BASE_URL}/otomobil")
        from selenium.webdriver.common.by import By
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC
        brand_select = WebDriverWait(self.driver, 10).until(
            EC.presence_of_element_located((By.ID, "brand"))
        )
        options = brand_select.find_elements(By.TAG_NAME, "option")
        brands = [opt.text for opt in options if opt.get_attribute("value")]
        return brands

    def get_models_for_brand(self, brand_name: str) -> List[str]:
        """Verilen markaya ait model listesini al."""
        if self.use_selenium:
            return self._get_models_selenium(brand_name)
        else:
            # API endpoint'i bulmak için deneme (örnek)
            # Sahibinden'in model endpoint'ini bulursanız buraya ekleyin
            return []

    def _get_models_selenium(self, brand_name: str) -> List[str]:
        from selenium.webdriver.support.ui import Select
        from selenium.webdriver.common.by import By
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC

        # Marka dropdown'ını bul ve seç
        brand_select_elem = WebDriverWait(self.driver, 10).until(
            EC.presence_of_element_located((By.ID, "brand"))
        )
        select = Select(brand_select_elem)
        select.select_by_visible_text(brand_name)

        # Model dropdown'ının güncellenmesini bekle
        model_select_elem = WebDriverWait(self.driver, 10).until(
            EC.presence_of_element_located((By.ID, "model"))
        )
        options = model_select_elem.find_elements(By.TAG_NAME, "option")
        models = [opt.text for opt in options if opt.get_attribute("value")]
        return models

    def get_all_car_models(self) -> List[Tuple[str, str]]:
        """(marka, model) tuple listesi döndür."""
        brands = self.get_brands()
        all_models = []
        for brand in brands:
            models = self.get_models_for_brand(brand)
            for model in models:
                all_models.append((brand, model))
            time.sleep(2)  # siteyi yormamak için bekle
        return all_models

    def close(self):
        if self.use_selenium and hasattr(self, 'driver'):
            self.driver.quit()

# ------------------------------------------------------------------
# Veritabanı işlemleri
# ------------------------------------------------------------------
class Database:
    def __init__(self, host="localhost", database="otoservisdb", user="root", password=""):
        """
        MySQL bağlantısı kurar.
        İleride Natro'ya taşırken host, user, password değiştirilebilir.
        """
        self.host = host
        self.database = database
        self.user = user
        self.password = password
        self.conn = None
        self.connect()
        self.create_tables()

    def connect(self):
        try:
            self.conn = mysql.connector.connect(
                host=self.host,
                database=self.database,
                user=self.user,
                password=self.password
            )
            logging.info("MySQL bağlantısı başarılı.")
        except Error as e:
            logging.error(f"MySQL bağlantı hatası: {e}")
            raise

    def create_tables(self):
        cursor = self.conn.cursor()
        # brands tablosu
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS brands (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(255) UNIQUE NOT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
        """)
        # models tablosu
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS models (
                id INT AUTO_INCREMENT PRIMARY KEY,
                brand_id INT NOT NULL,
                name VARCHAR(255) NOT NULL,
                FOREIGN KEY (brand_id) REFERENCES brands(id) ON DELETE CASCADE,
                UNIQUE KEY (brand_id, name)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
        """)
        # updates tablosu
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS updates (
                id INT AUTO_INCREMENT PRIMARY KEY,
                update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                status TEXT
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
        """)
        self.conn.commit()
        cursor.close()

    def insert_brand(self, name: str) -> int:
        cursor = self.conn.cursor()
        cursor.execute("INSERT IGNORE INTO brands (name) VALUES (%s)", (name,))
        self.conn.commit()
        cursor.execute("SELECT id FROM brands WHERE name = %s", (name,))
        brand_id = cursor.fetchone()[0]
        cursor.close()
        return brand_id

    def insert_model(self, brand_name: str, model_name: str):
        brand_id = self.insert_brand(brand_name)
        cursor = self.conn.cursor()
        cursor.execute("INSERT IGNORE INTO models (brand_id, name) VALUES (%s, %s)", (brand_id, model_name))
        self.conn.commit()
        cursor.close()

    def log_update(self, status: str):
        cursor = self.conn.cursor()
        cursor.execute("INSERT INTO updates (status) VALUES (%s)", (status,))
        self.conn.commit()
        cursor.close()

    def close(self):
        if self.conn:
            self.conn.close()
            logging.info("MySQL bağlantısı kapatıldı.")

# ------------------------------------------------------------------
# Güncelleme Zamanlayıcı
# ------------------------------------------------------------------
class Updater:
    def __init__(self, scraper, db, interval_hours=24):
        self.scraper = scraper
        self.db = db
        self.interval_hours = interval_hours

    def update(self):
        logging.info("Güncelleme başlatılıyor...")
        try:
            models = self.scraper.get_all_car_models()
            for brand, model in models:
                self.db.insert_model(brand, model)
            self.db.log_update("SUCCESS")
            logging.info(f"Güncelleme tamamlandı. {len(models)} model bulundu.")
        except Exception as e:
            logging.error(f"Güncelleme başarısız: {e}")
            self.db.log_update(f"FAILED: {e}")

    def run_forever(self):
        self.update()  # ilk çalıştırma
        schedule.every(self.interval_hours).hours.do(self.update)
        logging.info(f"Zamanlayıcı başlatıldı. Her {self.interval_hours} saatte bir güncellenecek.")
        while True:
            schedule.run_pending()
            time.sleep(1)

# ------------------------------------------------------------------
# Ana Çalıştırma
# ------------------------------------------------------------------
def main():
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler("car_model_updater.log"),
            logging.StreamHandler()
        ]
    )
    
    # Kullanıcı tarafından belirlenecek ayarlar
    USE_SELENIUM = True        # Sahibinden.com JavaScript kullanıyorsa True yapın
    # DB_PATH = "car_models.db"
    INTERVAL_HOURS = 24        # Güncelleme aralığı (saat)

    # MySQL ayarları (yerel geliştirme)
    DB_HOST = "localhost"
    DB_NAME = "otoservisdb"
    DB_USER = "root"
    DB_PASS = "Ccee3344!"   # şifrenizi buraya girin, boş bırakırsanız boş kalır

    scraper = CarModelScraper(use_selenium=USE_SELENIUM)
    db = Database(host=DB_HOST, database=DB_NAME, user=DB_USER, password=DB_PASS)
    updater = Updater(scraper, db, INTERVAL_HOURS)
    
    # scraper = CarModelScraper(use_selenium=USE_SELENIUM)
    # db = Database(DB_PATH)
    # updater = Updater(scraper, db, INTERVAL_HOURS)

    try:
        updater.run_forever()
    except KeyboardInterrupt:
        logging.info("Uygulama kullanıcı tarafından durduruldu.")
    finally:
        scraper.close()
        db.close()

if __name__ == "__main__":
    main()