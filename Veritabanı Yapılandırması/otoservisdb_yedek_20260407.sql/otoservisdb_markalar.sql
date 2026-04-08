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
-- Table structure for table `markalar`
--

DROP TABLE IF EXISTS `markalar`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `markalar` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT 'Marka tekil kimligi (PK)',
  `ad` varchar(100) DEFAULT NULL COMMENT 'Marka adi',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_markalar_ad` (`ad`),
  KEY `ix_markalar_id` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=42 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Sistemde tanimli olan arac markalarinin tutuldugu referans tablosu (Orn: Ford, BMW).';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `markalar`
--

LOCK TABLES `markalar` WRITE;
/*!40000 ALTER TABLE `markalar` DISABLE KEYS */;
INSERT INTO `markalar` VALUES (7,'Alfa Romeo'),(22,'Audi'),(17,'BMW'),(41,'Chery'),(9,'Chevrolet'),(26,'Chrysler'),(5,'Citroën'),(4,'Dacia'),(35,'Daewoo'),(25,'Dodge'),(36,'Fiat'),(27,'Ford'),(11,'Honda'),(28,'Hummer'),(29,'Hyundai'),(30,'Infiniti'),(31,'Jaguar'),(32,'Jeep'),(23,'Kia'),(24,'Land Rover'),(15,'Lexus'),(13,'Mazda'),(20,'Mercedes-Benz'),(37,'MINI'),(14,'Mitsubishi'),(33,'Nissan'),(6,'Opel'),(3,'Peugeot'),(10,'Porsche'),(2,'Renault'),(38,'Rover'),(21,'Saab'),(1,'Seat'),(8,'Škoda'),(39,'Smart'),(12,'Subaru'),(19,'Suzuki'),(40,'Togg'),(16,'Toyota'),(18,'Volkswagen'),(34,'Volvo');
/*!40000 ALTER TABLE `markalar` ENABLE KEYS */;
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
