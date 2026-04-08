CREATE DATABASE  IF NOT EXISTS `otoservisdb` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `otoservisdb`;
-- MySQL dump 10.13  Distrib 8.0.36, for Win64 (x86_64)
--
-- Host: localhost    Database: otoservisdb
-- ------------------------------------------------------
-- Server version	8.0.37

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `kullanicilar`
--

DROP TABLE IF EXISTS `kullanicilar`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `kullanicilar` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT 'Kullanici tekil kimligi (PK)',
  `ad_soyad` varchar(100) DEFAULT NULL COMMENT 'Kullanicinin tam adi',
  `eposta` varchar(100) DEFAULT NULL COMMENT 'Giris ve iletisim icin e-posta adresi',
  `telefon` varchar(20) DEFAULT NULL COMMENT 'Iletisim icin telefon numarasi',
  `sifre_hash` varchar(255) DEFAULT NULL COMMENT 'Guvenlik icin hashlenmis sifre',
  `aktif_mi` tinyint(1) DEFAULT NULL COMMENT 'Kullanici hesabi aktif mi? (Gecmisi silmemek / soft-delete icin)',
  `kayit_tarihi` datetime DEFAULT NULL COMMENT 'Hesabin olusturulma zamani',
  `adres` text,
  `silinme_tarihi` datetime DEFAULT NULL,
  `rol` varchar(20) DEFAULT 'Musteri',
  `fcm_token` varchar(255) DEFAULT NULL COMMENT 'Kullanıcının telefonuna anlık bildirim (Push Notification) göndermek için kullanılan Firebase cihaz kimliği',
  `son_giris_tarihi` datetime DEFAULT NULL COMMENT 'Kullanıcının sisteme son giriş yaptığı tarih',
  `mail_istiyor_mu` tinyint(1) DEFAULT '1' COMMENT 'KVKK Kapsamında mail alma izni',
  `son_hatirlatma_tarihi` datetime DEFAULT NULL COMMENT 'Her gün mail atıp spamlememek için son hatırlatma zamanı',
  PRIMARY KEY (`id`),
  UNIQUE KEY `telefon` (`telefon`),
  UNIQUE KEY `ix_kullanicilar_eposta` (`eposta`),
  KEY `ix_kullanicilar_id` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Sisteme kayitli musteri ve yoneticilerin bilgilerini tutan ana tablo.';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `kullanicilar`
--

LOCK TABLES `kullanicilar` WRITE;
/*!40000 ALTER TABLE `kullanicilar` DISABLE KEYS */;
INSERT INTO `kullanicilar` VALUES (1,'Çağatay Erdoğdu','erdogdu3434@gmail.com','1453','1',1,'2026-03-18 16:46:43','Ümraniye',NULL,'Admin','fsT5xdqyTtOThjEIrLfvz2:APA91bELevOedIi57bMlAz0_jw9En_n1poUT-bfevrXlBIlXdbBer-dmja4ql1mN0MdtbenwDxwC55F2rIP_LgPSzg9uKYyq1gzVxSX8pyc-L6Lv0MM9df8','2026-04-07 11:46:35',1,NULL),(2,'testUser1','testuser1@kapidanbakim.com','12345678','1',1,'2026-03-16 12:00:31','Test Sk',NULL,'Musteri',NULL,'2025-07-12 14:53:34',1,NULL),(3,'Test2','erdogdu_3434@hotmail.com','12345','1',1,'2026-03-18 10:03:48','Adres',NULL,'Musteri','egCPCMRTQiCsSBQjelUvuU:APA91bEjyEi2JzK1jbOwb7T1BI-ia8AHwXZae-RcxbdhepD39JY_rwQw8VTNC19egDr48M847Nx7tVgq3JiS0IsxBG8TjnfV7gisAQa_5Tj9u_YRoMYGuPM','2025-07-12 14:53:34',1,'2026-04-05 00:45:27'),(4,'trive','cagatay.erdogdu@trive.com','34','1',1,'2026-03-18 11:42:59','Ümraniye',NULL,'Musteri',NULL,'2025-07-12 14:53:34',1,'2026-04-04 17:27:46'),(12,'Pasif User','pasif@pasif.com','1','1',1,'2026-04-03 09:01:26',NULL,NULL,'Musteri',NULL,'2025-07-12 14:53:34',0,NULL),(14,'Aktif Test User Test','aktif@aktif.com','12','123456',1,'2026-04-03 09:11:37',NULL,'2026-04-06 18:06:55','Musteri','chYrfesjSc-OlLmgG7_Luo:APA91bHQXy49uc7EZLtpltAw1S1T8cB2qcUoims_VYr5n4da9RKrWvNDPv8t2swJMBTTo70pbTSiJe0gcRnpNsUMzFrz992lYcfBhOd3BVtjqYmuojpCpKI','2026-04-06 18:08:58',0,NULL);
/*!40000 ALTER TABLE `kullanicilar` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-07 14:25:53
