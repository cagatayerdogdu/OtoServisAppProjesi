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
-- Table structure for table `servis_talepleri`
--

DROP TABLE IF EXISTS `servis_talepleri`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `servis_talepleri` (
  `id` int NOT NULL AUTO_INCREMENT,
  `kullanici_id` int NOT NULL,
  `arac_id` int NOT NULL,
  `hizmet_id` int NOT NULL,
  `talep_tarihi` date NOT NULL,
  `adres` text NOT NULL,
  `notlar` text,
  `durum` varchar(50) DEFAULT 'Bekliyor',
  `onerilen_tarih` datetime DEFAULT NULL,
  `insert_tarihi` datetime DEFAULT CURRENT_TIMESTAMP,
  `guncelleme_tarihi` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `kayit_durumu` varchar(1) DEFAULT 'A' COMMENT 'A: Aktif, X: Silinmis',
  `silinme_tarihi` datetime DEFAULT NULL,
  `tahmini_tutar` float DEFAULT '0',
  `duzeltme_istendi_mi` tinyint(1) DEFAULT '0',
  `duzeltme_notu` text,
  `tamamlanma_tarihi` datetime DEFAULT NULL COMMENT 'Servis tamamlanma zamanı',
  `iptal_eden_id` int DEFAULT NULL COMMENT 'Talebi iptal eden kişinin IDsi',
  PRIMARY KEY (`id`),
  KEY `kullanici_id` (`kullanici_id`),
  KEY `arac_id` (`arac_id`),
  KEY `hizmet_id` (`hizmet_id`),
  CONSTRAINT `servis_talepleri_ibfk_1` FOREIGN KEY (`kullanici_id`) REFERENCES `kullanicilar` (`id`),
  CONSTRAINT `servis_talepleri_ibfk_2` FOREIGN KEY (`arac_id`) REFERENCES `araclar` (`id`),
  CONSTRAINT `servis_talepleri_ibfk_3` FOREIGN KEY (`hizmet_id`) REFERENCES `hizmetler` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=22 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `servis_talepleri`
--

LOCK TABLES `servis_talepleri` WRITE;
/*!40000 ALTER TABLE `servis_talepleri` DISABLE KEYS */;
INSERT INTO `servis_talepleri` VALUES (1,1,1,1,'2026-03-26','Adres1',NULL,'Tamamlandı',NULL,'2026-03-25 15:15:53','2026-03-26 16:19:14','A',NULL,4500,0,NULL,NULL,NULL),(2,1,2,25,'2026-03-25','Adres2',NULL,'Tamamlandı',NULL,'2026-03-25 15:16:18','2026-03-25 17:04:01','A',NULL,5500,0,NULL,NULL,NULL),(3,1,2,60,'2026-03-25','Adres3',NULL,'İşlemde',NULL,'2026-03-25 15:16:33','2026-03-29 19:14:27','A',NULL,15000,0,NULL,NULL,NULL),(4,1,1,28,'2026-03-25','adres4',NULL,'Tamamlandı',NULL,'2026-03-25 15:36:21','2026-03-25 17:04:01','A',NULL,15000,0,NULL,NULL,NULL),(5,1,2,33,'2026-03-25','Adres5',NULL,'İptal Edildi',NULL,'2026-03-25 16:09:31','2026-04-07 11:48:07','A','2026-03-25 17:04:01',5000,0,NULL,NULL,1),(6,1,3,8,'2026-03-25','adres','dikkat et usta','Tamamlandı',NULL,'2026-03-25 19:24:35','2026-04-06 19:25:47','A',NULL,3000,0,NULL,'2026-03-31 13:26:48',NULL),(7,1,1,47,'2026-03-26','Adres6',NULL,'İşlemde',NULL,'2026-03-26 13:04:06','2026-03-31 13:26:48','A',NULL,4000,1,'Talep1',NULL,NULL),(8,1,2,57,'2026-03-26','Adres','Test','Onaylandı',NULL,'2026-03-26 14:33:23','2026-04-07 11:52:41','A',NULL,1200,0,NULL,NULL,NULL),(9,1,3,19,'2026-03-27','adres',NULL,'İşlemde',NULL,'2026-03-27 23:55:34','2026-04-06 20:23:08','A',NULL,12000,0,NULL,NULL,NULL),(10,1,5,60,'2026-04-01','Ümraniye','usta','Tamamlandı',NULL,'2026-03-29 15:49:13','2026-04-06 19:58:29','A',NULL,3000,0,NULL,'2026-04-06 19:58:29',NULL),(11,2,1,1,'2026-03-30','adres yeni','yok','Onaylandı',NULL,'2026-03-30 20:05:36','2026-04-03 17:39:04','A',NULL,4500,0,NULL,NULL,NULL),(12,2,1,1,'2026-03-30','adres yeni','yok','İptal Edildi',NULL,'2026-03-30 20:05:39','2026-04-06 20:17:38','A',NULL,4500,0,NULL,NULL,NULL),(13,2,1,1,'2026-03-31','adres yeni','yok','İptal Edildi',NULL,'2026-03-30 21:14:23','2026-04-06 20:52:00','A',NULL,4500,0,NULL,NULL,NULL),(14,2,1,1,'2026-03-31','adres yeni','yok','İptal Edildi',NULL,'2026-03-30 22:11:44','2026-04-06 21:42:20','A',NULL,4500,0,NULL,NULL,NULL),(15,1,2,35,'2026-04-01','Ümraniye 4','Not ekledim 4','İptal Edildi',NULL,'2026-03-31 13:23:21','2026-04-06 20:19:42','A',NULL,1500,0,NULL,NULL,NULL),(16,2,1,1,'2026-04-03','adres yeni','yok','Bekliyor',NULL,'2026-04-02 10:52:43','2026-04-07 11:55:50','A',NULL,4500,0,NULL,NULL,NULL),(17,1,14,12,'2026-04-02','Ümraniye',NULL,'Tamamlandı',NULL,'2026-04-02 12:55:48','2026-04-06 21:42:33','A',NULL,2200,0,NULL,'2026-04-06 21:42:33',NULL),(18,1,15,12,'2026-04-04','Ümraniye','KULLANICI İPTALİ - KAYIT Durumu X','İptal Edildi',NULL,'2026-04-03 17:38:11','2026-04-07 11:48:07','X','2026-04-04 19:10:43',0,0,NULL,NULL,2),(19,1,2,35,'2026-04-06','Ümraniye',NULL,'Onaylandı',NULL,'2026-04-06 15:01:58','2026-04-06 21:59:56','A',NULL,1500,0,NULL,NULL,NULL),(20,1,2,57,'2026-04-06','Ümraniye',NULL,'Onaylandı',NULL,'2026-04-06 15:03:26','2026-04-07 11:08:44','A',NULL,1200,0,NULL,NULL,NULL),(21,1,2,57,'2026-04-06','Ümraniye','KULLANICI İPTALİ - KAYIT Durumu X','İptal Edildi',NULL,'2026-04-06 16:29:32','2026-04-07 11:48:07','X','2026-04-06 20:16:45',0,0,NULL,'2026-04-06 20:16:45',1);
/*!40000 ALTER TABLE `servis_talepleri` ENABLE KEYS */;
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
