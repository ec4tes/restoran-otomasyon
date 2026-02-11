# Restoran Otomasyonu

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?style=flat-square&logo=windows)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?style=flat-square&logo=sqlite)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

Modern ve kullanıcı dostu restoran yönetim sistemi. WPF ve .NET 8 ile geliştirilmiştir.

![Screenshot](docs/screenshot.png)

## ✨ Özellikler

- 🍽️ **Masa Yönetimi** - Masa durumları, bölüm bazlı görüntüleme
- 📝 **Sipariş Takibi** - Kolay sipariş alma ve düzenleme
- 🥡 **Gel-Al & Paket** - Masa dışı sipariş desteği
- ⚖️ **Yarım Porsiyon** - Esnek porsiyon seçenekleri
- 💰 **Anlık Fiyat Değiştirme** - Sipariş bazlı fiyat düzenleme
- 📊 **Toplu Fiyat Güncelleme** - Yüzdelik zam/indirim
- ⭐ **Favori Ürünler** - Hızlı erişim
- 🎁 **İkram Yönetimi** - İkram nedeni takibi
- 🧾 **Fiş Yazdırma** - Müşteri ve mutfak fişleri
- 📈 **Raporlar** - Günlük/aylık satış analizleri
- 👥 **Kullanıcı Yönetimi** - Admin ve Garson rolleri
- 🔄 **Sıfırlama Seçenekleri** - Veritabanı yönetimi

## 🚀 Kurulum

### İndirip Kullanma (Önerilen)

1. [Releases](../../releases) sayfasından son sürümü indirin
2. ZIP dosyasını çıkarın
3. `RestoranOtomasyon.exe` çalıştırın
4. Varsayılan PIN: `1234`

> ⚠️ .NET kurulumu gerekmez - Self-contained package

### Kaynak Koddan Derleme

```bash
# Klonla
git clone https://github.com/KULLANICI_ADI/restoran-otomasyon.git
cd restoran-otomasyon

# Derle ve çalıştır
cd RestoranOtomasyon/src/RestoranOtomasyon
dotnet run

# Release için publish
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 📋 Sistem Gereksinimleri

- Windows 10/11 (64-bit)
- 4GB RAM
- 200MB disk alanı

## 🛠️ Teknolojiler

- **.NET 8** - Framework
- **WPF** - UI Framework
- **SQLite** - Veritabanı
- **Dapper** - Micro ORM
- **CommunityToolkit.Mvvm** - MVVM Pattern
- **Serilog** - Logging

## 📁 Proje Yapısı

```
RestoranOtomasyon/
├── src/
│   └── RestoranOtomasyon/
│       ├── Data/           # Veritabanı işlemleri
│       ├── Models/         # Entity sınıfları
│       ├── Services/       # İş mantığı
│       ├── ViewModels/     # MVVM ViewModels
│       ├── Views/          # XAML UI
│       ├── Converters/     # Value Converters
│       └── Infrastructure/ # Altyapı kodları
└── docs/                   # Dokümantasyon
```

## 🎨 Ekran Görüntüleri

<details>
<summary>Masa Ekranı</summary>

![Masa Ekranı](docs/masalar.png)
</details>

<details>
<summary>Sipariş Ekranı</summary>

![Sipariş Ekranı](docs/siparis.png)
</details>

<details>
<summary>Yönetim Paneli</summary>

![Yönetim Paneli](docs/yonetim.png)
</details>

## 🔐 Varsayılan Giriş

| Rol | PIN |
|-----|-----|
| Admin | 1234 |

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/YeniOzellik`)
3. Değişikliklerinizi commit edin (`git commit -m 'Yeni özellik eklendi'`)
4. Branch'i push edin (`git push origin feature/YeniOzellik`)
5. Pull Request açın

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.

## 👨‍💻 Geliştirici

**CEMİLBEY Yazılım**

---

⭐ Projeyi beğendiyseniz yıldız vermeyi unutmayın!
