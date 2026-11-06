# API conventions

## Errors

Failures are returned as RFC 7807 `application/problem+json`. Domain errors carry a stable `code` extension
(for example `BOOKING_NOT_FOUND`) that the frontend should switch on, because `title` and `detail` are only
for humans. Model validation failures are `ValidationProblemDetails` with an `errors` map per field.

## Pagination

Lists that grow without bound are paged with an offset:

```
GET /api/bookings?page=2&pageSize=50&isCompleted=false
```

| Parameter | Default | Allowed |
|---|---|---|
| `page` | 1 | 1–100 000 |
| `pageSize` | 20 | 1–100 |

Out-of-range values return `400` instead of being clamped. A page past the end returns an empty `items`
array with the real `totalCount`.

The response is always the same envelope:

```json
{
  "items": [ ... ],
  "page": 2,
  "pageSize": 50,
  "totalCount": 137,
  "totalPages": 3
}
```

Every paged query has a fixed order that ends with the id, so rows never repeat or disappear between pages:

| Endpoint | Order |
|---|---|
| `GET /api/bookings` | `scheduledAt`, then id |
| `GET /api/clients` | `name`, then id |
| `GET /api/vehicle` | `licensePlate`, then id |
| `GET /api/inventory` | `name`, then id |

Small lookup lists (services, inventory categories, employees) are not paged; they return a plain array.

### Adding paging to an endpoint

1. Bind `[FromQuery] PageRequest paging` in the controller. Don't name the parameter `page`: MVC would then
   treat `?page=` as a prefix and silently ignore it.
2. Order the query, ending with a unique key, and call `ToPagedResponseAsync(paging)` from
   `Shared.Application.Pagination`.
3. Map entities to DTOs with `PagedResponse<T>.Map`.

The public marketplace search will use cursor (keyset) pagination instead, since offset gets slow on deep
pages and doesn't suit infinite scroll.

## Dates and times

- **Moments are UTC.** Responses carry ISO 8601 with `Z` (`"2026-10-03T08:00:00Z"`), and requests must send the same
  (e.g. `scheduledAt`, the `fromDate`/`toDate` booking filters). The database stores `timestamptz`.
- **Each studio has an IANA time zone** (`Europe/Warsaw` unless changed). `GET /api/studios/my` returns it as
  `timeZone`; the owner changes it with `PUT /api/studios/time-zone` `{ "timeZone": "Europe/Berlin" }`. Windows names
  (`Central European Standard Time`) are rejected with `INVALID_TIME_ZONE`.
- **Show times in the studio's zone, not the browser's**, so someone abroad still sees the studio's schedule:
  `new Intl.DateTimeFormat('pl-PL', { timeZone: studio.timeZone, dateStyle: 'short', timeStyle: 'short' })`.
  When the user picks a local date and time, convert it to UTC in that zone before sending it.
- **Calendar days are plain dates** (`"yyyy-MM-dd"`), meaning the studio's local days. The bookings report
  (`POST /api/reports/bookings`, `{ "dateFrom": "2026-10-01", "dateTo": "2026-10-31" }`, both days included) works this
  way; the server turns the days into UTC with the studio's zone, so a month is the studio's month even across a
  clock change.
- In code, convert with `Shared.Kernel.Time.TimeZones` (`DaysToUtc`, `ToLocal`), and use half-open ranges
  (`>= from && < toExclusive`) so consecutive days never share a booking.

## Services, categories and vehicle sizes

- Fixed lists travel as names (strings), never numbers; the frontend translates them for display.
  - Service categories: `ExteriorWash`, `Interior`, `Polishing`, `Ceramic`, `Ppf`, `WheelsAndTyres`, `Other`.
  - Vehicle sizes: `Small` (city car), `Medium` (compact/sedan), `Large` (SUV/estate), `XL` (van/pickup).
- A service has one category and an optional price per vehicle size (złoty, at most two decimals). A size without a
  price means "price on request".
  - `POST /api/service` `{ "name": "Pranie tapicerki", "category": "Interior", "prices": [{ "vehicleSize": "Large", "price": 349.99 }] }`.
    Only `name` is required: a service without the rest is `Other` with no prices.
  - `PUT /api/service/{id}` takes the same body. Left out, `category` and `prices` stay as they are; a sent `prices`
    list replaces the old one (`[]` clears it).
- CRM vehicles have a `size` (default `Medium`). Left out on edit, it stays as it is.

## Public endpoints (customer map)

- `GET /api/public/studios` and `GET /api/public/studios/{id}` need no login, never ask for a studio (no
  `X-Studio-Id`, even for staff of several studios), and are rate-limited per client IP
  (`RateLimiting:PublicPermitLimit` requests per minute, 120 by default; over it, 429).
- Search takes the visible map area and optional filters:
  `?minLat=50.0&maxLat=50.1&minLng=19.8&maxLng=20.1&category=Interior&vehicleSize=Large&page=1&pageSize=50`.
  It returns published studios as pins: `studioId`, `name`, `city`, `latitude`, `longitude`, `categories`, and
  `fromPrice` (the cheapest price for that vehicle size within the category; null means "price on request").
- Only published listings are visible; an unpublished or unknown studio is 404 `LISTING_NOT_FOUND`.
- The owner manages the listing with `GET/PUT /api/marketplace/listing` (name, description, address, city, phone,
  `latitude`/`longitude` from a pin on a map) and `POST /api/marketplace/listing/publish` or `/unpublish`.
  A new listing copies the studio's current services; later service changes follow automatically.
  Publishing needs at least one service (`LISTING_HAS_NO_SERVICES`).
- Behind the production reverse proxy, the API takes the client IP from `X-Forwarded-For` (trusted from loopback
  and private addresses only), so the proxy must set it. Nginx needs
  `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;`; Caddy sends it by default.
