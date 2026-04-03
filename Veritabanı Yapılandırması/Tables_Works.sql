select * from otoservisdb.markalar;
select * from otoservisdb.modeller;
select * from otoservisdb.kullanicilar;
select * from otoservisdb.servis_talepleri;
select * from otoservisdb.araclar;
select * from otoservisdb.hizmetler;
select * from otoservisdb.hizmet_fiyat_gecmisi;
select * from otoservisdb.sistem_loglari;
select * from otoservisdb.sistem_bildirimleri;

/*
SET FOREIGN_KEY_CHECKS = 0;    
	truncate table otoservisdb.araclar;
	truncate table otoservisdb.hizmetler;
	truncate table otoservisdb.hizmet_fiyat_gecmisi;
	truncate table otoservisdb.markalar;
    truncate table otoservisdb.modeller;
	truncate table otoservisdb.servis_talepleri;
SET FOREIGN_KEY_CHECKS = 1;


SET FOREIGN_KEY_CHECKS = 0;   
UPDATE `otoservisdb`.`kullanicilar` SET `id` = '2' WHERE (`id` = '1');
UPDATE `otoservisdb`.`kullanicilar` SET `id` = '1' WHERE (`id` = '7');
UPDATE `otoservisdb`.`kullanicilar` SET `id` = '3' WHERE (`id` = '2');
UPDATE `otoservisdb`.`kullanicilar` SET `id` = '4' WHERE (`id` = '6');
UPDATE `otoservisdb`.`kullanicilar` SET `id` = '5' WHERE (`id` = '8');
SET FOREIGN_KEY_CHECKS = 1;
*/
/*
update otoservisdb.kullanicilar
set sifre_hash = '1'
where id in (1,2);

update otoservisdb.kullanicilar
set sifre_hash = '1'
where id in (1, 2, 6, 7);
*/