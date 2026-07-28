# ADR 0001: Modüler monolit ve katman bağımlılıkları

- Durum: Kabul edildi
- Tarih: 2026-07-28

## Bağlam

Platform; kimlik doğrulama, durak/güzergâh yönetimi, rota üretimi ve ileride canlı araç takibi içerecek. İlk sürümde ayrı servislerin dağıtım ve veri tutarlılığı maliyetine ihtiyaç yoktur; ancak iş kurallarının web ve veritabanı ayrıntılarından korunması gerekir.

## Karar

Backend .NET 10 üzerinde modüler monolit olarak geliştirilecektir:

```text
Transport.Api
    ├─ Transport.Application
    └─ Transport.Infrastructure
           ├─ Transport.Application
           └─ Transport.Domain

Transport.Application
    └─ Transport.Domain

Transport.Domain
    └─ başka uygulama katmanına bağımlı değil
```

Bağımlılık yönü architecture testleriyle korunacaktır. API yalnızca HTTP ve composition root görevini, Infrastructure dış sistem bağlantılarını, Application kullanım senaryolarını, Domain ise temel iş kurallarını üstlenir.

## Sonuçlar

- İş kuralları PostgreSQL, MapLibre veya HTTP sözleşmesinden bağımsız kalır.
- Tek process ile dağıtım ve yerel geliştirme kolaylaşır.
- Canlı takip ileride ölçek gerektirirse kendi servisine ayrılabilir.
- Katmanlar arasında DTO ve adaptör sınırlarını koruma disiplini gerekir.
