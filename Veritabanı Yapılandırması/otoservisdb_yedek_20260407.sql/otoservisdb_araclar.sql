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
-- Table structure for table `araclar`
--

DROP TABLE IF EXISTS `araclar`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `araclar` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT 'Arac tekil kimligi (PK)',
  `sahip_id` int DEFAULT NULL COMMENT 'Aracin sahibinin ID''si (FK)',
  `marka_id` int DEFAULT NULL COMMENT 'Sistemden secilen marka ID''si (FK)',
  `model_id` int DEFAULT NULL COMMENT 'Sistemden secilen model ID''si (FK)',
  `ozel_marka` varchar(100) DEFAULT NULL COMMENT 'Eger marka listede yoksa kullanicinin manuel girdigi marka',
  `ozel_model` varchar(100) DEFAULT NULL COMMENT 'Eger model listede yoksa kullanicinin manuel girdigi model',
  `yil` int DEFAULT NULL COMMENT 'Aracin uretim yili',
  `yakit_tipi` varchar(30) DEFAULT NULL COMMENT 'Benzin, Dizel, Elektrik vb. (Ileride tabloya donusebilir)',
  `kilometre` int DEFAULT NULL COMMENT 'Aracin anlik kilometresi',
  `kayit_tarihi` datetime DEFAULT NULL COMMENT 'Aracin sisteme eklenme zamani',
  `kayit_durumu` varchar(1) DEFAULT 'A' COMMENT 'A: Aktif, X: Silinmis',
  `silinme_tarihi` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `sahip_id` (`sahip_id`),
  KEY `marka_id` (`marka_id`),
  KEY `model_id` (`model_id`),
  KEY `ix_araclar_id` (`id`),
  CONSTRAINT `araclar_ibfk_1` FOREIGN KEY (`sahip_id`) REFERENCES `kullanicilar` (`id`),
  CONSTRAINT `araclar_ibfk_2` FOREIGN KEY (`marka_id`) REFERENCES `markalar` (`id`),
  CONSTRAINT `araclar_ibfk_3` FOREIGN KEY (`model_id`) REFERENCES `modeller` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Musterilere ait araclarin donanim, marka ve model bilgilerinin tutuldugu tablo.';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `araclar`
--

LOCK TABLES `araclar` WRITE;
/*!40000 ALTER TABLE `araclar` DISABLE KEYS */;
INSERT INTO `araclar` VALUES (1,1,3,895,NULL,NULL,2024,'Benzin',30000,'2026-03-25 12:10:24','A',NULL),(2,1,27,641,NULL,NULL,2018,'Dizel',159700,'2026-03-25 12:13:21','A',NULL),(3,1,20,513,NULL,NULL,2025,'Elektrik',18000,'2026-03-25 16:23:38','A',NULL),(4,1,22,547,NULL,NULL,2024,'Elektrik',35000,'2026-03-26 11:38:54','A',NULL),(5,1,22,525,NULL,NULL,2021,'Dizel',20000,'2026-03-29 12:48:24','A',NULL),(6,1,35,806,NULL,NULL,2023,'Hibrit',45000,'2026-03-29 14:18:35','A',NULL),(7,1,11,219,NULL,NULL,2024,'Elektrik',15000,'2026-03-29 14:58:18','A',NULL),(8,1,1,11,NULL,NULL,2015,'Benzin',180000,'2026-03-29 15:11:53','A',NULL),(9,1,17,352,NULL,NULL,2025,'Elektrik',10,'2026-03-29 15:44:32','A',NULL),(10,1,29,676,NULL,NULL,1998,'Benzin',205000,'2026-03-29 16:12:38','A',NULL),(11,2,1,1,'','',2019,'Benzin',9000,'2026-03-30 17:05:02','A',NULL),(12,1,34,804,NULL,NULL,2026,'Hibrit',0,'2026-03-31 10:22:17','A',NULL),(13,2,2,2,'','',2022,'Benzin',22000,'2026-04-01 09:58:16','A',NULL),(14,1,24,598,NULL,NULL,2020,'Benzin',109000,'2026-04-02 09:55:16','A',NULL),(15,1,7,160,NULL,NULL,2025,'Elektrik',10000,'2026-04-03 14:37:54','X','2026-04-04 19:10:52');
/*!40000 ALTER TABLE `araclar` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-07 14:25:51
