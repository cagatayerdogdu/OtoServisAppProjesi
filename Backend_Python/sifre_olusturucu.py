from cryptography.fernet import Fernet

# 1. Aşılmaz bir Anahtar (Key) oluşturuyoruz
anahtar = Fernet.generate_key()
print(f"BUNU SUNUCU PANELINE SAKLAYACAKSIN (ENCRYPTION_KEY):\n{anahtar.decode()}\n")

# 2. Gerçek şifreni bu anahtarla şifreliyoruz
cipher_suite = Fernet(anahtar)
gercek_sifre = b"Ccee3344!" # Gerçek DB şifren
sifrelenmis_metin = cipher_suite.encrypt(gercek_sifre)

print(f"BUNU .env DOSYASINA YAZACAKSIN (DB_PASSWORD):\n{sifrelenmis_metin.decode()}")