# MeldenIT Agent API Dokümantasyonu

## Genel Bilgiler

MeldenIT Agent API, Windows ajanları ile merkez sunucu arasındaki iletişimi sağlayan RESTful API'dir.

- **Base URL**: `https://assit.meldencloud.com`
- **API Version**: v1
- **Authentication**: Bearer Token
- **Content-Type**: application/json

## Authentication

Tüm API istekleri için Bearer token authentication kullanılır:

```http
Authorization: Bearer <device_token>
```

## Endpoints

### 1. Agent Registration

Agent'ın sisteme kaydolması için kullanılır.

**Endpoint**: `POST /api/v1/agents/register`

**Request Body**:
```json
{
  "agent_guid": "string",
  "hostname": "string",
  "serial": "string",
  "domain": "string",
  "version": "string",
  "site_code": "string"
}
```

**Response**:
```json
{
  "device_token": "string",
  "policy": {
    "heartbeat_interval": 15,
    "delta_sync_interval": 360,
    "full_sync_time": "03:00",
    "max_retry_attempts": 3,
    "retry_delay_seconds": 30
  }
}
```

**Status Codes**:
- `200` - Başarılı kayıt
- `400` - Geçersiz istek
- `500` - Sunucu hatası

### 2. Heartbeat

Agent'ın canlılığını bildirmek için kullanılır.

**Endpoint**: `POST /api/v1/agents/heartbeat`

**Request Body**:
```json
{
  "agent_guid": "string",
  "version": "string",
  "last_sync": "2024-01-01T00:00:00Z",
  "status": "healthy"
}
```

**Response**:
```json
{
  "status": "success",
  "message": "Heartbeat received",
  "config_updated": false
}
```

**Status Codes**:
- `200` - Başarılı
- `400` - Geçersiz istek
- `404` - Agent bulunamadı

### 3. Inventory Sync

Envanter verilerinin senkronizasyonu için kullanılır.

#### Delta Sync

**Endpoint**: `POST /api/v1/inventory/delta`

**Request Body**:
```json
{
  "agent_guid": "string",
  "sync_type": "delta",
  "inventory": {
    "device_identity": {
      "hostname": "string",
      "serial_number": "string",
      "domain": "string",
      "ou": "string",
      "sid": "string",
      "uuid": "string",
      "asset_tag": "string",
      "logged_in_user": "string"
    },
    "hardware": {
      "manufacturer": "string",
      "model": "string",
      "cpu": {
        "name": "string",
        "cores": 0,
        "logical_processors": 0,
        "max_clock_speed": 0
      },
      "memory": {
        "total_gb": 0.0,
        "slots": [
          {
            "capacity_gb": 0.0,
            "speed_mhz": 0,
            "manufacturer": "string",
            "part_number": "string"
          }
        ]
      },
      "disks": [
        {
          "model": "string",
          "capacity_gb": 0.0,
          "type": "string",
          "partitions": [
            {
              "drive_letter": "string",
              "size_gb": 0.0,
              "free_space_gb": 0.0,
              "file_system": "string"
            }
          ]
        }
      ],
      "gpu": {
        "name": "string",
        "memory_mb": 0,
        "driver_version": "string"
      }
    },
    "software": {
      "os_name": "string",
      "os_version": "string",
      "os_build": "string",
      "installed_software": [
        {
          "name": "string",
          "version": "string",
          "publisher": "string",
          "install_date": "2024-01-01T00:00:00Z"
        }
      ],
      "dotnet_versions": ["string"]
    },
    "network": {
      "adapters": [
        {
          "name": "string",
          "mac_address": "string",
          "connection_type": "string",
          "is_connected": true
        }
      ],
      "ipv4_addresses": ["string"],
      "ipv6_addresses": ["string"],
      "gateway": "string",
      "dns_servers": ["string"],
      "wifi_ssid": "string"
    },
    "bios": {
      "name": "string",
      "version": "string",
      "release_date": "2024-01-01T00:00:00Z",
      "tpm_enabled": true,
      "secure_boot": true
    },
    "usage": {
      "uptime_hours": 0.0,
      "last_reboot": "2024-01-01T00:00:00Z",
      "cpu_usage_avg": 0.0,
      "memory_usage_avg": 0.0,
      "disk_io_avg": 0.0
    },
    "tagging": {
      "location": "string",
      "site_code": "string",
      "department": "string",
      "cost_center": "string"
    },
    "collected_at": "2024-01-01T00:00:00Z"
  },
  "last_sync": "2024-01-01T00:00:00Z"
}
```

#### Full Sync

**Endpoint**: `POST /api/v1/inventory/full`

**Request Body**: Delta sync ile aynı format

**Response**:
```json
{
  "status": "success",
  "message": "Inventory synced successfully",
  "snipeit_updated": true,
  "next_sync": "2024-01-01T06:00:00Z"
}
```

**Status Codes**:
- `200` - Başarılı senkronizasyon
- `400` - Geçersiz istek
- `404` - Agent bulunamadı
- `500` - Sunucu hatası

### 4. Update Check

Agent güncellemelerini kontrol etmek için kullanılır.

**Endpoint**: `POST /api/v1/update/check`

**Request Body**:
```json
{
  "agent_guid": "string",
  "current_version": "string"
}
```

**Response**:
```json
{
  "update_available": true,
  "latest_version": "1.1.0",
  "download_url": "https://assit.meldencloud.com/downloads/agent-1.1.0.msi",
  "release_notes": "Bug fixes and improvements",
  "force_update": false
}
```

**Status Codes**:
- `200` - Başarılı
- `400` - Geçersiz istek

### 5. Agent Configuration

Agent konfigürasyonunu almak için kullanılır.

**Endpoint**: `GET /api/v1/agents/{agent_guid}/config`

**Response**:
```json
{
  "heartbeat_interval": 15,
  "delta_sync_interval": 360,
  "full_sync_time": "03:00",
  "max_retry_attempts": 3,
  "retry_delay_seconds": 30
}
```

**Status Codes**:
- `200` - Başarılı
- `404` - Agent bulunamadı

## Error Handling

### Error Response Format

```json
{
  "error": "string",
  "message": "string",
  "details": "string",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### Common Error Codes

- `400 Bad Request` - Geçersiz istek formatı
- `401 Unauthorized` - Kimlik doğrulama hatası
- `403 Forbidden` - Yetki hatası
- `404 Not Found` - Kaynak bulunamadı
- `429 Too Many Requests` - Rate limit aşıldı
- `500 Internal Server Error` - Sunucu hatası

## Rate Limiting

API istekleri için rate limiting uygulanır:

- **Heartbeat**: 10 istek/dakika
- **Inventory Sync**: 5 istek/dakika
- **Update Check**: 1 istek/saat
- **Registration**: 1 istek/dakika

## Data Models

### Device Identity

```json
{
  "hostname": "string",
  "serial_number": "string",
  "domain": "string",
  "ou": "string",
  "sid": "string",
  "uuid": "string",
  "asset_tag": "string",
  "logged_in_user": "string"
}
```

### Hardware Info

```json
{
  "manufacturer": "string",
  "model": "string",
  "cpu": {
    "name": "string",
    "cores": 0,
    "logical_processors": 0,
    "max_clock_speed": 0
  },
  "memory": {
    "total_gb": 0.0,
    "slots": [
      {
        "capacity_gb": 0.0,
        "speed_mhz": 0,
        "manufacturer": "string",
        "part_number": "string"
      }
    ]
  },
  "disks": [
    {
      "model": "string",
      "capacity_gb": 0.0,
      "type": "string",
      "partitions": [
        {
          "drive_letter": "string",
          "size_gb": 0.0,
          "free_space_gb": 0.0,
          "file_system": "string"
        }
      ]
    }
  ],
  "gpu": {
    "name": "string",
    "memory_mb": 0,
    "driver_version": "string"
  }
}
```

### Software Info

```json
{
  "os_name": "string",
  "os_version": "string",
  "os_build": "string",
  "installed_software": [
    {
      "name": "string",
      "version": "string",
      "publisher": "string",
      "install_date": "2024-01-01T00:00:00Z"
    }
  ],
  "dotnet_versions": ["string"]
}
```

### Network Info

```json
{
  "adapters": [
    {
      "name": "string",
      "mac_address": "string",
      "connection_type": "string",
      "is_connected": true
    }
  ],
  "ipv4_addresses": ["string"],
  "ipv6_addresses": ["string"],
  "gateway": "string",
  "dns_servers": ["string"],
  "wifi_ssid": "string"
}
```

### BIOS Info

```json
{
  "name": "string",
  "version": "string",
  "release_date": "2024-01-01T00:00:00Z",
  "tpm_enabled": true,
  "secure_boot": true
}
```

### Usage Info

```json
{
  "uptime_hours": 0.0,
  "last_reboot": "2024-01-01T00:00:00Z",
  "cpu_usage_avg": 0.0,
  "memory_usage_avg": 0.0,
  "disk_io_avg": 0.0
}
```

### Tagging Info

```json
{
  "location": "string",
  "site_code": "string",
  "department": "string",
  "cost_center": "string"
}
```

## Examples

### Complete Agent Registration Flow

```bash
# 1. Register agent
curl -X POST "https://assit.meldencloud.com/api/v1/agents/register" \
  -H "Content-Type: application/json" \
  -d '{
    "agent_guid": "12345678-1234-1234-1234-123456789012",
    "hostname": "WORKSTATION-01",
    "serial": "ABC123456789",
    "domain": "company.local",
    "version": "1.0.0",
    "site_code": "HQ"
  }'

# 2. Send heartbeat
curl -X POST "https://assit.meldencloud.com/api/v1/agents/heartbeat" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <device_token>" \
  -d '{
    "agent_guid": "12345678-1234-1234-1234-123456789012",
    "version": "1.0.0",
    "status": "healthy"
  }'

# 3. Sync inventory
curl -X POST "https://assit.meldencloud.com/api/v1/inventory/full" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <device_token>" \
  -d '{
    "agent_guid": "12345678-1234-1234-1234-123456789012",
    "sync_type": "full",
    "inventory": { ... },
    "last_sync": null
  }'
```

### Error Handling Example

```json
{
  "error": "ValidationError",
  "message": "Invalid request data",
  "details": "Field 'agent_guid' is required",
  "timestamp": "2024-01-01T00:00:00Z"
}
```
