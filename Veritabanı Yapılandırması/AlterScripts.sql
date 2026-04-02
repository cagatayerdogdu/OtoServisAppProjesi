
-- ALTER TABLE otoservisdb.kullanicilar ADD COLUMN adres TEXT;

-- Önce eski tablolar varsa temizleyelim (Hata vermemesi için önce alt tablo silinir)
DROP TABLE IF EXISTS hizmet_fiyat_gecmisi;
DROP TABLE IF EXISTS hizmetler;

-- 1. Hizmetler Ana Tablosu (Tarih damgalı)
CREATE TABLE otoservisdb.hizmetler (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ad VARCHAR(100) NOT NULL,
    aciklama TEXT,
    varsayilan_fiyat DECIMAL(10,2) NOT NULL,
    onceki_fiyat DECIMAL(10,2) NULL,
    insert_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
    guncelleme_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 2. Fiyat Geçmişi (Arşiv) Tablosu (Sadece insert_tarihi yeterli çünkü bu log tablosudur, update edilmez)
CREATE TABLE otoservisdb.hizmet_fiyat_gecmisi (
    id INT AUTO_INCREMENT PRIMARY KEY,
    hizmet_id INT NOT NULL,
    eski_fiyat DECIMAL(10,2) NOT NULL,
    yeni_fiyat DECIMAL(10,2) NOT NULL,
    insert_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (hizmet_id) REFERENCES hizmetler(id) ON DELETE CASCADE
);

-- 3. İlk Test Verilerini Ekliyoruz
INSERT INTO otoservisdb.hizmetler (ad, aciklama, varsayilan_fiyat, onceki_fiyat) VALUES 
('Periyodik Bakım (Yağ/Filtre)', 'Motor yağı ve tüm filtrelerin değişimi', 2500.00, 2000.00),
('Ön Fren Balatası Değişimi', 'Orijinal veya eşdeğer balata takımı', 1200.00, 1000.00),
('Akü Değişimi (Mutlu/Varta)', 'Eski akü alınıp yenisi takılır', 1800.00, 1500.00),
('Genel Arıza Tespiti', 'Bilgisayarlı check-up ve ustalık kontrolü', 500.00, 400.00);


DROP TABLE IF EXISTS otoservisdb.servis_talepleri;
CREATE TABLE otoservisdb.servis_talepleri (
    id INT AUTO_INCREMENT PRIMARY KEY,
    kullanici_id INT NOT NULL,
    arac_id INT NOT NULL,
    hizmet_id INT NOT NULL,
    talep_tarihi DATE NOT NULL,
    adres TEXT NOT NULL,
    notlar TEXT,
    durum VARCHAR(50) DEFAULT 'Bekliyor',
    onerilen_tarih DATETIME NULL,
    insert_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
    guncelleme_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id),
    FOREIGN KEY (arac_id) REFERENCES araclar(id),
    FOREIGN KEY (hizmet_id) REFERENCES hizmetler(id)
);

-- '1', '1', '1', 'Standart Bakım', '1', 'Namık Kemal mh. No.19', '1050', 'Beklemede', '2026-03-16 12:11:09', '2026-03-16 12:11:09'


CREATE TABLE otoservisdb.sistem_loglari (
    id INT AUTO_INCREMENT PRIMARY KEY,
    kullanici_id INT NULL,
    seviye VARCHAR(20) DEFAULT 'ERROR',
    islem VARCHAR(100) NOT NULL,
    detay TEXT NOT NULL,
    insert_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP
);



-- Sistem Logları Tablosu Yorumları
ALTER TABLE otoservisdb.sistem_loglari COMMENT = 'Uygulama genelindeki kritik hatalari (500) ve traceback ciktilarini tutan log tablosu.';
ALTER TABLE otoservisdb.sistem_loglari MODIFY id INT AUTO_INCREMENT COMMENT 'Log tekil kimligi (PK)',
MODIFY kullanici_id INT NULL COMMENT 'Hata aninda islem yapan kullanici (Varsa)',
MODIFY seviye VARCHAR(20) DEFAULT 'ERROR' COMMENT 'Log seviyesi (ERROR, WARNING, INFO)',
MODIFY islem VARCHAR(100) NOT NULL COMMENT 'Hatanin alindigi Endpoint/URL',
MODIFY detay TEXT NOT NULL COMMENT 'Hatanin teknik detayi ve Traceback metni',
MODIFY insert_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT 'Hatanin olustugu tarih ve saat';

-- Hizmetler Tablosu Yorumları
ALTER TABLE otoservisdb.hizmetler COMMENT = 'Musterilere sunulan bakim ve onarim hizmetlerinin fiyat listesi.';
ALTER TABLE otoservisdb.hizmetler MODIFY id INT AUTO_INCREMENT COMMENT 'Hizmet tekil kimligi (PK)',
MODIFY ad VARCHAR(100) NOT NULL COMMENT 'Hizmetin vitrin adi',
MODIFY aciklama TEXT COMMENT 'Hizmet icerigi ve detaylari',
MODIFY varsayilan_fiyat DECIMAL(10,2) NOT NULL COMMENT 'Mevcut guncel satis fiyati',
MODIFY onceki_fiyat DECIMAL(10,2) NULL COMMENT 'Bir onceki satis fiyati',
MODIFY insert_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT 'Hizmetin sisteme eklenme tarihi',
MODIFY guncelleme_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Fiyat veya detayin son degisim tarihi';

-- Fiyat Geçmişi Tablosu Yorumları
ALTER TABLE otoservisdb.hizmet_fiyat_gecmisi COMMENT = 'Hizmet fiyatlarindaki degisimleri tarihsel olarak tutan log/arsiv tablosu.';
ALTER TABLE otoservisdb.hizmet_fiyat_gecmisi MODIFY id INT AUTO_INCREMENT COMMENT 'Arsiv kaydi tekil kimligi (PK)',
MODIFY hizmet_id INT NOT NULL COMMENT 'Fiyati degisen hizmetin ID''si (FK)',
MODIFY eski_fiyat DECIMAL(10,2) NOT NULL COMMENT 'Degisim oncesi fiyat',
MODIFY yeni_fiyat DECIMAL(10,2) NOT NULL COMMENT 'Degisim sonrasi yeni fiyat',
MODIFY insert_tarihi DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT 'Degisimin yapildigi tarih ve saat';

ALTER TABLE otoservisdb.kullanicilar ADD COLUMN kayit_durumu VARCHAR(1) DEFAULT 'A' COMMENT 'A: Aktif, X: Silinmis';
ALTER TABLE otoservisdb.kullanicilar ADD COLUMN silinme_tarihi DATETIME NULL;

ALTER TABLE otoservisdb.araclar ADD COLUMN kayit_durumu VARCHAR(1) DEFAULT 'A' COMMENT 'A: Aktif, X: Silinmis';
ALTER TABLE otoservisdb.araclar ADD COLUMN silinme_tarihi DATETIME NULL;

ALTER TABLE otoservisdb.servis_talepleri ADD COLUMN kayit_durumu VARCHAR(1) DEFAULT 'A' COMMENT 'A: Aktif, X: Silinmis';
ALTER TABLE otoservisdb.servis_talepleri ADD COLUMN silinme_tarihi DATETIME NULL;

-- 1. Tabloya rol kolonunu ekle (Varsayılan olarak herkes Müşteri olacak)
ALTER TABLE otoservisdb.kullanicilar ADD COLUMN rol VARCHAR(20) DEFAULT 'Musteri';

-- 2. Kendi hesabını Admin yap (Buradaki e-posta adresini kendi giriş yaptığın e-posta ile değiştir!)
UPDATE otoservisdb.kullanicilar SET rol = 'Admin' WHERE eposta = 'erdogdu3434@gmail.com';

ALTER TABLE otoservisdb.servis_talepleri ADD COLUMN tahmini_tutar FLOAT DEFAULT 0;

ALTER TABLE otoservisdb.servis_talepleri ADD COLUMN duzeltme_istendi_mi BOOLEAN DEFAULT FALSE;
ALTER TABLE otoservisdb.servis_talepleri ADD COLUMN duzeltme_notu TEXT;

ALTER TABLE otoservisdb.kullanicilar 
ADD COLUMN fcm_token VARCHAR(255) NULL COMMENT 'Kullanıcının telefonuna anlık bildirim (Push Notification) göndermek için kullanılan Firebase cihaz kimliği';