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
-- Table structure for table `sistem_bildirimleri`
--

DROP TABLE IF EXISTS `sistem_bildirimleri`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sistem_bildirimleri` (
  `id` int NOT NULL AUTO_INCREMENT,
  `kullanici_id` int NOT NULL,
  `baslik` varchar(100) NOT NULL,
  `mesaj` varchar(500) NOT NULL,
  `okundu_mu` tinyint(1) DEFAULT NULL,
  `olusturulma_tarihi` datetime DEFAULT (now()),
  PRIMARY KEY (`id`),
  KEY `kullanici_id` (`kullanici_id`),
  KEY `ix_sistem_bildirimleri_id` (`id`),
  CONSTRAINT `sistem_bildirimleri_ibfk_1` FOREIGN KEY (`kullanici_id`) REFERENCES `kullanicilar` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=64 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sistem_bildirimleri`
--

LOCK TABLES `sistem_bildirimleri` WRITE;
/*!40000 ALTER TABLE `sistem_bildirimleri` DISABLE KEYS */;
INSERT INTO `sistem_bildirimleri` VALUES (5,1,'Talebiniz Güncellendi','Servis talebiniz \'Onaylandı\' aşamasına geçmiştir.',1,'2026-03-29 15:56:59'),(6,1,'Talebiniz Güncellendi','Servis talebiniz \'Onaylandı\' aşamasına geçmiştir.',1,'2026-03-29 16:09:07'),(7,1,'Talebiniz Güncellendi','Servis talebiniz \'Bekliyor\' aşamasına geçmiştir.',1,'2026-03-29 18:14:18'),(8,1,'Talebiniz Güncellendi','Servis talebiniz \'Bekliyor\' aşamasına geçmiştir.',1,'2026-03-29 18:18:55'),(9,1,'Müşteri Düzeltme Talebi','Talep ID: 3 için müşteri düzeltme istiyor. Not: test6',1,'2026-03-29 19:13:19'),(10,1,'Müşteri Düzeltme Talebi','Talep ID: 3 için müşteri düzeltme istiyor. Not: test6',1,'2026-03-29 19:13:25'),(11,1,'Müşteri Düzeltme Talebi','Talep ID: 3 için müşteri düzeltme istiyor. Not: test6',1,'2026-03-29 19:13:27'),(12,1,'Talebiniz Güncellendi','Servis talebiniz \'Bekliyor\' aşamasına geçmiştir.',1,'2026-03-29 19:14:13'),(13,1,'Talebiniz Güncellendi','Servis talebiniz \'İşlemde\' aşamasına geçmiştir.',1,'2026-03-29 19:14:27'),(14,1,'Talebiniz Güncellendi','Servis talebiniz \'Onaylandı\' aşamasına geçmiştir.',1,'2026-03-29 19:14:42'),(15,2,'Talebiniz Güncellendi','Servis talebiniz \'Onaylandı\' aşamasına geçmiştir.',0,'2026-03-30 20:08:28'),(16,1,'Müşteri Düzeltme Talebi','Talep ID: 11 için müşteri düzeltme istiyor. Not: düzeltme 1',1,'2026-03-30 20:10:41'),(17,1,'Müşteri Düzeltme Talebi','Talep ID: 11 için müşteri düzeltme istiyor. Not: düzeltme 2',1,'2026-03-30 21:13:50'),(18,1,'Yeni Servis Talebi','testUser1 adlı müşteri \'Standart Periyodik Bakım\' için yeni bir servis talebi oluşturdu.',1,'2026-03-30 21:14:23'),(19,1,'Yeni Servis Talebi','testUser1 adlı müşteri \'Standart Periyodik Bakım\' için yeni bir servis talebi oluşturdu.',1,'2026-03-30 22:11:47'),(20,1,'Müşteri Düzeltme Talebi','Talep ID: 11 için müşteri düzeltme istiyor. Not: düzeltme 3',1,'2026-03-30 22:11:58'),(21,1,'Yeni Servis Talebi','Çağatay Erdoğdu adlı müşteri \'Cam Suyu ve Silecek Lastiği Değişimi\' için yeni bir servis talebi oluşturdu.',1,'2026-03-31 13:23:27'),(22,1,'Talebiniz Güncellendi','Servis talebiniz \'İşlemde\' aşamasına geçmiştir.',1,'2026-03-31 13:24:02'),(23,1,'Müşteri Düzeltme Talebi','Talep ID: 7 için müşteri düzeltme istiyor. Not: Talep1',1,'2026-03-31 13:26:48'),(24,1,'Müşteri Düzeltme Talebi','Müşteri testUser1, Araç ID: 1 talebi için düzeltme istiyor. Not: Yeni Düzeltme',1,'2026-03-31 15:56:38'),(25,1,'Müşteri Düzeltme Talebi','Müşteri testUser1, Araç ID: 1 aracı için \'Standart Periyodik Bakım\' (Talep ID: 11) talebine düzeltme istiyor. Not: Yeni Düzeltme 2',1,'2026-03-31 17:51:08'),(26,1,'Müşteri Düzeltme Talebi','Müşteri testUser1, Peugeot 3008 aracı için \'Standart Periyodik Bakım\' (Talep ID: 11) talebine düzeltme istiyor. Not: Yeni Düzeltme Son',1,'2026-03-31 19:10:59'),(27,1,'Müşteri Talebini Güncelledi','Talep ID: 15 \'li Araç: Daewoo Kalos için açılan Hizmet: Cam Suyu ve Silecek Lastiği Değişimi Çağatay Erdoğdu kullanıcısı tarafından düzeltildi.',1,'2026-03-31 19:12:25'),(28,1,'Müşteri Talebini Güncelledi','Talep ID: 15 \'li Araç: Daewoo Kalos için açılan Hizmet: Cam Suyu ve Silecek Lastiği Değişimi Çağatay Erdoğdu kullanıcısı tarafından düzeltildi.',1,'2026-03-31 19:12:38'),(29,1,'Müşteri Düzeltme Talebi','Müşteri testUser1, Peugeot 3008 aracı için \'Standart Periyodik Bakım\' (Talep ID: 11) talebine düzeltme istiyor. Not: Yeni Düzeltme 20260401',1,'2026-04-01 12:56:15'),(30,1,'Müşteri Düzeltme Talebi','Müşteri testUser1, Peugeot 3008 aracı için \'Standart Periyodik Bakım\' (Talep ID: 11) talebine düzeltme istiyor. Not: Yeni Düzeltme 20260401',1,'2026-04-01 13:00:20'),(31,1,'Müşteri Talebini Güncelledi','Talep ID: 15 için Çağatay Erdoğdu şu detayları güncelledi: Randevu Tarihi, Müşteri Notu.',1,'2026-04-01 13:03:08'),(32,1,'Müşteri Talebini Güncelledi','Talep ID: 15 için Çağatay Erdoğdu şu detayları güncelledi: Hizmet (ABS Beyni Tamiri), Araç Bilgisi.',1,'2026-04-01 13:03:45'),(33,1,'Müşteri Düzeltme Talebi','Müşteri testUser1, Peugeot 3008 aracı için \'Standart Periyodik Bakım\' (Talep ID: 11) talebine düzeltme istiyor. Not: Yeni Düzeltme 20260401',1,'2026-04-01 13:26:57'),(34,1,'Müşteri Talebini Güncelledi','(Talep ID: 15 ) \'ABS Beyni Tamiri\' için Çağatay Erdoğdu şu detayları güncelledi: Hizmet (Ağır Bakım (Triger Seti)), Araç Bilgisi (Mercedes-Benz), Randevu Tarihi None, Adres, Müşteri Notu.',1,'2026-04-01 13:32:22'),(35,1,'Müşteri Düzeltme Talebi','Müşteri testUser1, Peugeot 3008 aracı için \'Standart Periyodik Bakım\' (Talep ID: 11) talebine düzeltme istiyor. Not: Yeni Düzeltme 20260401',1,'2026-04-01 14:41:27'),(36,1,'Müşteri Talebini Güncelledi','(Talep ID: 15 ) \'Ağır Bakım (Triger Seti)\' için Çağatay Erdoğdu şu detayları güncelledi: Hizmet (Akü Değişimi (72 Ah Standart)), Araç Bilgisi (Ford Focus), Randevu Tarihi (2026-04-01), Adres (Ümraniye 3), Müşteri Notu (Not ekledim 3).',1,'2026-04-01 14:42:11'),(37,1,'Müşteri Talebini Güncelledi','(Talep ID: 15 ) \'Akü Değişimi (72 Ah Standart)\' için Çağatay Erdoğdu şu detayları güncelledi: Hizmet (Boğaz Kelebeği Temizliği), Araç Bilgisi (Mercedes-Benz Sprinter).',1,'2026-04-01 15:49:55'),(38,1,'Müşteri Düzeltme Talebi','Müşteri Çağatay Erdoğdu, Ford Focus aracı için \'Far Ampulü / Xenon Değişimi\' (Talep ID: 8) talebine düzeltme istiyor. Not: test',1,'2026-04-01 15:51:03'),(39,1,'Müşteri Talebini Güncelledi','(Talep ID: 15 ) \'Boğaz Kelebeği Temizliği\' için Çağatay Erdoğdu şu detayları güncelledi: Adres (Ümraniye 4), Müşteri Notu (Not ekledim 4).',1,'2026-04-01 18:35:12'),(40,1,'Müşteri Talebini Güncelledi','(Talep ID: 15 ) \'Boğaz Kelebeği Temizliği\' için Çağatay Erdoğdu şu detayları güncelledi: Araç Bilgisi (Ford Focus).',1,'2026-04-01 19:17:25'),(48,2,'Talebiniz Güncellendi','Servis talebiniz \'Onaylandı\' aşamasına geçmiştir.',0,'2026-04-03 17:39:04'),(53,2,'Talebiniz Güncellendi','Servis talebiniz \'İptal Edildi\' aşamasına geçmiştir.',0,'2026-04-06 20:17:38'),(56,2,'Talebiniz Güncellendi','Servis talebiniz \'İptal Edildi\' aşamasına geçmiştir.',0,'2026-04-06 20:52:01'),(57,2,'Talebiniz Güncellendi','Servis talebiniz \'İptal Edildi\' aşamasına geçmiştir.',0,'2026-04-06 21:42:20'),(59,1,'Talebiniz Güncellendi','Servis talebiniz \'Onaylandı\' aşamasına geçmiştir.',1,'2026-04-06 21:59:56'),(60,1,'Talebiniz Güncellendi','Servis talebiniz \'Onaylandı\' aşamasına geçmiştir.',1,'2026-04-07 11:08:45'),(63,2,'Talebiniz Güncellendi','Servis talebiniz \'Bekliyor\' aşamasına geçmiştir.',0,'2026-04-07 11:55:50');
/*!40000 ALTER TABLE `sistem_bildirimleri` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-07 14:25:52
