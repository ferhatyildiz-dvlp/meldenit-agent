# MeldenIT Agent - Snipe-IT Entegrasyonlu Envanter Ajanı

## Genel Bakış

MeldenIT Agent, Windows bilgisayarlardan envanter verilerini toplayıp Snipe-IT'e entegre eden kapsamlı bir çözümdür. Sistem, Windows Service ve System Tray UI ile çalışan bir ajan, merkez sunucu API'si ve Snipe-IT entegrasyonu içerir.

## Mimari

```mermaid
graph TB
    A[Windows Agent] --> B[Merkez Sunucu API]
    B --> C[Snipe-IT]
    B --> D[PostgreSQL]
    
    A1[Service Component] --> A
    A2[Tray UI] --> A
    A3[Inventory Collector] --> A
    A4[Config Manager] --> A
    
    B1[FastAPI Backend] --> B
    B2[Auth Service] --> B
    B3[Snipe-IT Proxy] --> B
    B4[Data Sync] --> B
```

## Proje Yapısı

```
meldenit-agent-1/
├── agent/                    # Windows Agent (.NET 8)
│   ├── MeldenIT.Agent.Core/  # Ortak sınıflar ve modeller
│   ├── MeldenIT.Agent.Service/ # Windows Service
│   ├── MeldenIT.Agent.Tray/  # System Tray UI
│   ├── MeldenIT.Agent/       # Ana uygulama
│   └── MeldenIT.Agent.Tests/ # Unit testler
├── backend/                  # Backend API (Python FastAPI)
│   ├── app/                  # Uygulama kodu
│   ├── migrations/           # Veritabanı migration'ları
│   ├── tests/                # Backend testleri
│   └── scripts/              # Yardımcı scriptler
├── deployment/               # Deployment dosyaları
│   ├── wix/                  # MSI installer
│   ├── scripts/              # Kurulum scriptleri
│   └── config/               # Konfigürasyon dosyaları
├── docs/                     # Dokümantasyon
├── tests/                    # Integration ve E2E testler
├── scripts/                  # Genel scriptler
├── docker-compose.yml        # Docker Compose
└── README.md                 # Bu dosya
```

## Bileşenler

### 1. Windows Agent (C# .NET 8)
- **Service Component**: Windows Service olarak çalışan arka plan servisi
- **Tray UI**: System tray'de görünen kullanıcı arayüzü
- **Inventory Collector**: WMI ve PowerShell ile envanter verisi toplama
- **Config Manager**: Konfigürasyon ve kimlik yönetimi

### 2. Merkez Sunucu (Python FastAPI)
- **Backend API**: Agent'ların iletişim kurduğu merkez API
- **Snipe-IT Proxy**: Snipe-IT REST API'sine proxy katmanı
- **Auth Service**: JWT tabanlı kimlik doğrulama
- **Data Sync**: Envanter verilerinin Snipe-IT'e senkronizasyonu

### 3. Veritabanı (PostgreSQL)
- Agent kayıtları ve durumları
- Envanter verileri ve değişiklik geçmişi
- Audit logları ve sistem kayıtları

## Kurulum

### Windows Agent Kurulumu

```powershell
# MSI ile sessiz kurulum
msiexec /i MeldenITAgent.msi /qn API_URL="https://assit.meldencloud.com" SNIPEIT_URL="https://assit.meldencloud.com" SITE_CODE="MLDHQ"
```

### Sunucu Kurulumu

```bash
# Docker Compose ile kurulum
docker-compose up -d
```

## Konfigürasyon

### Agent Konfigürasyonu
Konfigürasyon dosyası: `C:\ProgramData\MeldenIT\Agent\agent.json`

```json
{
  "api_url": "https://assit.meldencloud.com",
  "snipeit_url": "https://assit.meldencloud.com",
  "site_code": "MLDHQ",
  "heartbeat_interval": 15,
  "delta_sync_interval": 360,
  "full_sync_time": "03:00"
}
```

### Sunucu Konfigürasyonu
`.env` dosyası ile yapılandırma:

```env
DATABASE_URL=postgresql://user:password@localhost/meldenit
SNIPEIT_API_TOKEN=your_snipeit_token
JWT_SECRET_KEY=your_jwt_secret
```

## API Endpoints

### Agent Endpoints
- `POST /api/agents/register` - Agent kaydı
- `POST /api/agents/heartbeat` - Canlılık kontrolü
- `POST /api/inventory/delta` - Delta envanter gönderimi
- `POST /api/inventory/full` - Tam envanter gönderimi
- `GET /api/agents/{id}/config` - Konfigürasyon çekme
- `GET /api/update/check` - Güncelleme kontrolü

### Snipe-IT Endpoints
- `POST /api/v1/hardware` - Donanım oluşturma
- `PATCH /api/v1/hardware/{id}` - Donanım güncelleme
- `GET /api/v1/hardware?search=` - Donanım arama

## Zamanlama

- **Heartbeat**: Her 15 dakikada bir
- **Delta Sync**: Her 6 saatte bir (değişiklikler)
- **Full Sync**: Günde 1 kez (03:00)
- **Manuel Sync**: Tray UI üzerinden

## Güvenlik

- TLS 1.2+ zorunlu
- JWT tabanlı kimlik doğrulama
- Token'lar Windows Credential Manager'da şifreli saklanır
- Sertifika doğrulama
- Proxy desteği

## Sorun Giderme

### Log Dosyaları
- Windows Event Log: `Application` log'unda `MeldenIT Agent` kaynaklı
- Dosya logları: `C:\ProgramData\MeldenIT\Agent\logs\`

### Yaygın Sorunlar
1. **Bağlantı sorunları**: Proxy ayarlarını kontrol edin
2. **Kimlik doğrulama**: Token'ları yenileyin
3. **Senkronizasyon**: Manuel sync deneyin

## Geliştirme

### Gereksinimler
- .NET 8 SDK
- Python 3.11+
- Docker & Docker Compose
- PostgreSQL 14+
- WiX Toolset (MSI oluşturmak için)

### Hızlı Başlangıç

#### Backend Geliştirme
```bash
# Backend bağımlılıklarını kur
cd backend
pip install -r requirements.txt

# Veritabanını başlat
docker-compose up -d postgres

# Migration'ları çalıştır
python scripts/init_db.py

# Backend'i başlat
uvicorn app.main:app --reload
```

#### Agent Geliştirme
```bash
# Agent'ı derle
cd agent
dotnet build

# Testleri çalıştır
dotnet test

# Tray uygulamasını çalıştır
dotnet run --project MeldenIT.Agent.Tray
```

### Test Çalıştırma
```bash
# Backend testleri
cd backend && python -m pytest

# Agent testleri
cd agent && dotnet test

# Integration testleri
cd tests && python -m pytest
```

### Build ve Deploy
```bash
# Agent MSI oluştur
cd deployment/scripts
./build.ps1 -Publish -CreateMsi

# Backend Docker image oluştur
docker-compose build

# Tüm servisleri deploy et
./scripts/deploy.sh deploy
```

## Dokümantasyon

- [Mimari Dokümantasyonu](docs/ARCHITECTURE.md)
- [API Dokümantasyonu](docs/API.md)
- [Deployment Dokümantasyonu](docs/DEPLOYMENT.md)

## Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit yapın (`git commit -m 'Add amazing feature'`)
4. Push yapın (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## Lisans

Proprietary - MeldenIT
