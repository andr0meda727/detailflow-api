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
