# API Uçları

> Güncel ve ayrıntılı kaynak Swagger'dır: `http://localhost:5269/swagger` (yalnızca `Development` ortamında açıktır). Bu dosya controller'lardaki `[Route]`, `[Http*]` ve `[Authorize]` tanımlarından üretilmiştir.

Tüm uçlar `api/` ön ekiyle başlar ve `{parametre}` biçimindeki ifadeler route parametresidir. "Gereken izin" sütunundaki değerler `RestaurantApi.Domain/Constants/Permissions.cs` içindeki sabitlerdir; `SameUser` ise izin değil, `Infrastructure/Security/Authorization/Extension/AuthorizationExtension.cs` içinde tanımlı özel bir policy'dir (kullanıcı yalnızca kendi kaynağına erişebilir). Public uçlar "Yok" olarak işaretlenmiştir.

## Auth — `api/auth`

| Method | Route | Açıklama | Gereken izin | Başarı kodu |
| --- | --- | --- | --- | --- |
| POST | `api/auth/register` | Yeni kullanıcı kaydı (doğrulama maili tetiklenir) | Yok | 201 |
| POST | `api/auth/login` | Kullanıcı girişi; refresh token `_session` cookie'sine yazılır | Yok | 200 |
| POST | `api/auth/mail-verify` | Mail doğrulama kodunun onaylanması | Yok | 200 |
| POST | `api/auth/two-factor/login` | İki faktörlü giriş adımı | Yok | 200 |
| POST | `api/auth/refresh` | `_session` cookie'sindeki refresh token ile yeni access token | Yok | 200 |
| POST | `api/auth/forgot-password/apply` | Şifre sıfırlama talebi (kod maili gönderilir) | Yok | 200 |
| POST | `api/auth/forgot-password/verify` | Şifre sıfırlama kodunun doğrulanması | Yok | 200 |
| POST | `api/auth/logout` | Refresh token iptali ve `_session` cookie'sinin silinmesi | Yok | 200 |

## Users — `api/users`

| Method | Route | Açıklama | Gereken izin | Başarı kodu |
| --- | --- | --- | --- | --- |
| GET | `api/users/me` | Oturumdaki kullanıcının bilgileri | `SameUser` | 200 |

## Profile — `api/profile`

| Method | Route | Açıklama | Gereken izin | Başarı kodu |
| --- | --- | --- | --- | --- |
| GET | `api/profile` | Profil bilgisi | `SameUser` | 200 |
| POST | `api/profile/change-password` | Şifre değiştirme | `SameUser` | 200 |

## Addresses — `api/addresses`

| Method | Route | Açıklama | Gereken izin | Başarı kodu |
| --- | --- | --- | --- | --- |
| POST | `api/addresses` | Yeni adres ekleme | `SameUser` | 201 |
| GET | `api/addresses` | Kullanıcının adres listesi | `SameUser` | 200 |
| GET | `api/addresses/{addressId}` | Adres detayı | `SameUser` | 200 |
| PATCH | `api/addresses/{addressId}` | Adres güncelleme | `SameUser` | 200 |
| DELETE | `api/addresses/{addressId}` | Adres silme | `SameUser` | 200 |

## Categories — `api/categories`

| Method | Route | Açıklama | Gereken izin | Başarı kodu |
| --- | --- | --- | --- | --- |
| POST | `api/categories` | Kategori oluşturma (görsel form-data ile gönderilir) | `category:create` | 201 |
| GET | `api/categories/public` | Public kategori listesi | Yok | 200 |
| GET | `api/categories/admin` | Admin kategori listesi (silinmişler dahil) | `category:view` | 200 |
| GET | `api/categories/{categoryId}` | Kategori detayı | `category:view` | 200 |
| PATCH | `api/categories/{categoryId}` | Kategori güncelleme (başlık ve/veya görsel) | `category:update` | 200 |
| DELETE | `api/categories/{categoryId}` | Kategoriyi soft delete etme | `category:delete` | 200 |

## Products — `api/products`

| Method | Route | Açıklama | Gereken izin | Başarı kodu |
| --- | --- | --- | --- | --- |
| GET | `api/products/public` | Public ürün listesi | Yok | 200 |
| GET | `api/products/admin` | Admin ürün listesi (silinmişler dahil) | `product:view` | 200 |
| GET | `api/products/public/{slug}` | Public ürün detayı (slug ile) | Yok | 200 |
| GET | `api/products/admin/{productId}` | Admin ürün detayı | `product:view` | 200 |
| POST | `api/products` | Ürün oluşturma (çoklu görsel form-data ile gönderilir) | `product:create` | 201 |
| PATCH | `api/products/{productId}` | Ürün güncelleme (alanlar, yeni görseller, silinecek görseller) | `product:update` | 200 |
| DELETE | `api/products/{productId}` | Ürünü soft delete etme (ilişkili indirim pasife alınır) | `product:delete` | 200 |

## Discounts — `api/discounts`

| Method | Route | Açıklama | Gereken izin | Başarı kodu |
| --- | --- | --- | --- | --- |
| GET | `api/discounts` | İndirim listesi; `status` (`upcoming` / `active` / `passive`) filtresi | `discount:view` | 200 |
| GET | `api/discounts/{discountId}` | İndirim detayı | `discount:view` | 200 |
| POST | `api/discounts` | İndirim oluşturma | `discount:create` | 201 |
| PATCH | `api/discounts/{discountId}` | İndirim güncelleme | `discount:update` | 200 |

## Tables — `api/tables`

| Method | Route | Açıklama | Gereken izin | Başarı kodu |
| --- | --- | --- | --- | --- |
| GET | `api/tables` | Masa listesi; `is-active`, `min-guest`, `max-guest`, `number` filtreleri | `table:view` | 200 |
| GET | `api/tables/available` | Müsait masalar; `guest-count`, `start-time`, `end-time` zorunludur (aktif rezervasyonu olmayan, kapasitesi yeterli ve aktif masalar) | `table:view` | 200 |
| GET | `api/tables/{tableId}` | Masa detayı | `table:view` | 200 |
| POST | `api/tables` | Masa oluşturma | `table:create` | 201 |
| PATCH | `api/tables/{tableId}` | Masa güncelleme (numara, kapasite, aktiflik) | `table:update` | 200 |
| DELETE | `api/tables/{tableId}` | Masayı pasife alma (`IsActive = false`) | `table:delete` | 200 |

## Diagnostics — `api/diagnostics`

| Method | Route | Açıklama | Gereken izin | Başarı kodu |
| --- | --- | --- | --- | --- |
| GET | `api/diagnostics/cache` | Redis cache hit/miss sayaçları | Yok | 200 |

## Ortak notlar

- **Yetkilendirme:** JWT Bearer token'ı `Authorization: Bearer <token>` başlığıyla gönderilir. İzin tabanlı policy'ler `Permissions` sabitlerinden üretilir; `SameUser` policy'si kullanıcının yalnızca kendi kaynağına erişmesini sağlar.
- **Hata kodları:** Doğrulama hataları 400, iş kuralı hataları 404 (`NotFoundException`), 409 (`ConflictException`) ve 422 (`UnprocessableEntityError`) olarak döner. Eşleme `WebApi/Middlewares/ExceptionMiddleware.cs` içinde yapılır.
- **Yanıt gövdesi:** Başarılı yanıtlar `BaseResponse` (`Message`, `Code`) veya veri içeren uçlarda `GeneralSuccessResponseWithData<T>` (`Message`, `Code`, `Data`) biçimindedir.
- **Kimlik doğrulama çerezleri:** Login, refresh ve logout uçları refresh token'ı `HttpOnly` `_session` cookie'sinde taşır; access token yanıt gövdesinde döner.
