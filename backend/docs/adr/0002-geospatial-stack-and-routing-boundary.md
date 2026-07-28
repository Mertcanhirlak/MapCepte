# ADR 0002: PostGIS, MapLibre ve rota motoru sınırı

- Durum: Kabul edildi; rota motoru saha spike'ı bekliyor
- Tarih: 2026-07-28

## Bağlam

Duraklar nokta, üretilen rotalar gerçek yol ağına oturan çizgi geometrileridir. Aynı güzergâha birden fazla rota bağlanabilmeli ve her rota haritada bağımsız katman olmalıdır.

## Karar

- Kalıcı coğrafi veri PostgreSQL + PostGIS içinde SRID 4326 ile saklanacaktır.
- EF Core, Npgsql ve NetTopologySuite C# ile PostGIS arasındaki dönüşümü sağlayacaktır.
- Frontend haritayı MapLibre ile oluşturacaktır.
- Açık kaynak rota motoru doğrudan domain kodundan çağrılmayacak; bir routing adaptörü arkasında bulunacaktır.
- PostgreSQL ve PostGIS ilk aşamada Windows üzerinde doğrudan yerel servis olarak çalıştırılacaktır.
- İlk aday OSRM'dir. Gerçek otobüs kısıtlarını doğrulamak için en az 10 duraklı saha spike'ı yapılmadan üretim motoru kesinleşmiş sayılmaz.
- Rota üretilene kadar sıralı duraklar arasında düz çizgi çizilmeyecektir.

## Katman sırası

1. Temel harita
2. Rota çizgileri
3. Durak noktaları
4. Seçim/vurgu
5. Canlı araçlar

## Sonuçlar

- Mekânsal indeksleme ve bbox sorguları veritabanında yapılabilir.
- Rota motoru değişikliği domain ve frontend sözleşmesini bozmaz.
- OSM tile ve yol verisinin lisans/attribution koşulları dağıtım öncesi ayrıca doğrulanmalıdır.
- OSRM verisi hazırlanmadığında rota servisi çalışamaz; bu nedenle rota motoru kurulumu PostGIS temelinden ayrı ele alınacaktır.
