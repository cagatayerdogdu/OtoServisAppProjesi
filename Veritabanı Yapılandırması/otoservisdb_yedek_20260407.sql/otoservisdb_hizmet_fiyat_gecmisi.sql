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
-- Table structure for table `hizmet_fiyat_gecmisi`
--

DROP TABLE IF EXISTS `hizmet_fiyat_gecmisi`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hizmet_fiyat_gecmisi` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT 'Arsiv kaydi tekil kimligi (PK)',
  `hizmet_id` int NOT NULL COMMENT 'Fiyati degisen hizmetin ID''si (FK)',
  `eski_fiyat` decimal(10,2) NOT NULL COMMENT 'Degisim oncesi fiyat',
  `yeni_fiyat` decimal(10,2) NOT NULL COMMENT 'Degisim sonrasi yeni fiyat',
  `insert_tarihi` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'Degisimin yapildigi tarih ve saat',
  PRIMARY KEY (`id`),
  KEY `hizmet_id` (`hizmet_id`),
  CONSTRAINT `hizmet_fiyat_gecmisi_ibfk_1` FOREIGN KEY (`hizmet_id`) REFERENCES `hizmetler` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Hizmet fiyatlarindaki degisimleri tarihsel olarak tutan log/arsiv tablosu.';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hizmet_fiyat_gecmisi`
--

LOCK TABLES `hizmet_fiyat_gecmisi` WRITE;
/*!40000 ALTER TABLE `hizmet_fiyat_gecmisi` DISABLE KEYS */;
INSERT INTO `hizmet_fiyat_gecmisi` VALUES (1,1,4500.10,4500.00,'2026-04-02 13:40:33'),(2,1,4500.00,4500.01,'2026-04-03 19:07:47'),(3,1,4500.01,4500.00,'2026-04-03 19:07:59');
/*!40000 ALTER TABLE `hizmet_fiyat_gecmisi` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-07 14:25:50
