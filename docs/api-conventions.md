# API conventions

## Errors
Endpoint by endpoint (bodies, responses, error codes): see [api-reference.md](api-reference.md).


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

The public map search stays on offset paging: it only returns the studios inside the visible map area, so the list
is short and moving the map starts a new search from page 1.

### Cursor (keyset) paging

A studio's public reviews are a "show more" list that grows at the top. With offsets, a review written while someone
reads would push one they already saw onto the next page, so this list pages by cursor instead:

```
GET /api/public/studios/{id}/reviews?limit=20
→ { "items": [ ... ], "nextCursor": "NjM4OTk..." }
GET /api/public/studios/{id}/reviews?limit=20&cursor=NjM4OTk...
→ { "items": [ ... ], "nextCursor": null }
```

The cursor is opaque (base64url of the last row's `createdAt` and id). There is no `totalCount` and no jumping to a
page; the studio's `reviewCount` gives the total. A malformed cursor returns `400` with `INVALID_CURSOR`.

To page another list this way: bind `[FromQuery] CursorRequest paging` (not named `cursor`, for the same prefix reason),
decode `paging.Cursor` with `TimeCursor.TryDecode`, keep the rows after it (moment desc, then id desc), order the same
way and call `ToCursorResponseAsync`.

## Caching

The public studio endpoints (`GET /api/public/studios`, `/{id}` and `/{id}/reviews`) are output-cached on the server
for up to 60 seconds, keyed by the full URL and shared by every caller. A cached answer carries an `Age` header. Any
write in the Marketplace module (a listing edit, publishing, a review, a price change arriving from Scheduling) empties
the cache, so changes made through the API show at once. Errors are never cached, and availability is not cached at all.
Nothing tenant-scoped is cached on the server.

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
- A service also has `durationMinutes` (5–720, default 60): how long it takes on a Medium car. Left out on edit, it
  stays. Other sizes are scaled by the studio's planning multipliers (see Planning).
- CRM vehicles have a `size` (default `Medium`). Left out on edit, it stays as it is. Bookings keep a copy of it
  (`vehicle.size` in the booking response).

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

## Customer accounts (`/api/me`)

- A customer is any signed-in Supabase account; customers sign up with Supabase from the frontend, and the
  `handle_new_user` trigger creates their profile row. They belong to no studio, and `/api/me` endpoints never ask
  for one (staff can use them too, for their own car).
- `GET/PUT /api/me/profile` `{ "name": "Anna Nowak", "phone": "500 600 700" }`: the contact details that go on booking
  requests. Before the first save, GET is 404 `CUSTOMER_PROFILE_NOT_FOUND`.
- `GET/POST /api/me/vehicles`, `PUT/DELETE /api/me/vehicles/{id}`
  `{ "licensePlate": "KR 12345", "brand": "Volvo", "model": "XC90", "size": "Large" }`: the garage. `size` is required
  (it sets the price), there are at most ten cars (`CUSTOMER_GARAGE_FULL`), one per plate (`CUSTOMER_VEHICLE_EXISTS`).
- Everything is limited to the caller's own rows: another customer's car ID is 404 `CUSTOMER_VEHICLE_NOT_FOUND`.

## Online booking requests

The customer asks, the studio answers; nothing is booked until staff accept.

Customer (signed in, `/api/me`, no studio needed):
- `POST /api/public/studios/{studioId}/estimate` `{ "serviceIds": [..], "vehicleSize": "Large" }` (no login): `services`
  with each `price` (null = on request), `total` of the priced ones, `hasUnpricedServices`.
- `POST /api/me/booking-requests`
  `{ "studioId", "serviceIds": [..], "vehicleId": "<garage car>" | "vehicle": { "licensePlate", "brand", "model", "size" },
  "preferredAt": "2026-11-20T08:00:00Z", "notes" }` → 201 with the request (`status: "Pending"`, `estimatedPrice`).
  The name and phone come from `/api/me/profile`, the email from the account. Prices are always the studio's own, worked
  out on the server. Errors: `STUDIO_NOT_FOUND` (not published), `CUSTOMER_PROFILE_REQUIRED`, `CUSTOMER_VEHICLE_NOT_FOUND`,
  `BOOKING_REQUEST_UNKNOWN_SERVICES`, `BOOKING_REQUEST_LIMIT` (three pending per studio). Rate-limited like the map.
  At a studio that plans, the whole visit must fit in its opening hours (`OUTSIDE_OPENING_HOURS`) and start after its
  lead time (`BOOKING_REQUEST_TOO_SOON`); offer the times from the public availability endpoint.
- `GET /api/me/booking-requests` (paged, newest first): the customer's history at every studio. Each item has `status`
  (`Pending`, `Accepted`, `Rejected`, `Withdrawn`), `rejectionReason`, and for accepted ones `booking` (its current
  `scheduledAt`, `status`, `totalPrice`).
- `POST /api/me/booking-requests/{id}/withdraw` while pending.

Studio staff (normal studio context):
- `GET /api/booking-requests?status=Pending` (paged; without `status`, pending first, then newest).
- `GET /api/booking-requests/{id}/plan` (studios that plan): whether the preferred time fits and on which bay, the five
  closest times that do, and other pending requests for the same time. See Planning.
- `POST /api/booking-requests/{id}/accept` `{ "scheduledAt"?, "price"?, "bayId"? }`: creates a normal booking (preferred time and
  estimate unless given) for the CRM client and car matched to the customer (by account, then phone, else created).
  In a studio that plans, the car goes on `bayId` or the best-fitting free bay; when none is free it is 409
  `NO_BAY_AVAILABLE` (or `BAY_TAKEN` for a chosen bay) and the request stays pending.
- `POST /api/booking-requests/{id}/reject` `{ "reason"? }`.
- Answering twice is `BOOKING_REQUEST_ALREADY_DECIDED`; another studio's request is `BOOKING_REQUEST_NOT_FOUND`.

Emails: the studio's owners get each new request; the customer gets the answer. Times are in the studio's time zone.

## Reviews

- A customer can review a visit once staff complete a booking that came from their request (bookings staff entered
  have no customer account). It becomes reviewable a moment after completion (on the bus), for 90 days.
- `GET /api/me/reviews/pending`: the caller's visits still to review (`bookingId`, `studioId`, `studioName`,
  `completedAt`, `reviewableUntil`).
- `POST /api/me/reviews` `{ "bookingId", "rating": 1-5, "comment"? }` → 201 with `authorName` ("Anna N." from the
  profile). Errors: `REVIEWABLE_VISIT_NOT_FOUND` (not their visit, not completed, or no customer),
  `REVIEW_ALREADY_EXISTS`, `REVIEW_PERIOD_OVER`, `CUSTOMER_PROFILE_REQUIRED`.
- `GET /api/public/studios/{studioId}/reviews` (no login, paged, newest first). The studio page and map pins carry
  `averageRating` (one decimal, null before the first review) and `reviewCount`.

## Planning (bays, opening hours, free times)

A studio **plans** once it has opening hours and at least one active bay (`planningEnabled` in the responses). Until
then bookings work as before: they get an end time but no bay, and nothing is checked.

Setup (owner writes, staff read):
- `GET/POST /api/bays`, `PUT /api/bays/{id}` `{ "name": "Stanowisko 1" }`, `DELETE /api/bays/{id}` (deactivates; refused
  with `BAY_HAS_UPCOMING_BOOKINGS` while open bookings are still to come on it). At most 20 active bays.
- `GET/PUT /api/schedule`
  `{ "openingHours": [{ "day": "Monday", "opens": "08:00:00", "closes": "16:00:00" }], "bufferMinutes": 15,
  "slotStepMinutes": 15, "minLeadHours": 12, "durationMultipliers": { "small": 0.85, "medium": 1, "large": 1.25, "xl": 1.5 } }`.
  Hours are the studio's local time; several periods a day make a break; overlaps are `OVERLAPPING_OPENING_PERIODS`.
  The buffer is cleanup time kept free around every visit on a bay; start times sit on the `slotStepMinutes` grid
  (15, 30 or 60); `minLeadHours` only applies to customers. An empty `openingHours` switches planning off.
- `POST /api/schedule/closures` `{ "date": "2026-12-24", "reason": "Wigilia" }`, `DELETE /api/schedule/closures/{date}`.

How long a visit takes: the services' `durationMinutes` added up, times the multiplier for the car's size, rounded up
to the grid. A booking's `endsAt` is always worked out this way, never sent.

Bookings (`POST/PUT /api/bookings` take an optional `bayId`; the response has `endsAt` and `bay`):
- A chosen bay is taken if it is free, buffer included, whatever the hours (staff may book outside them).
- Otherwise the planner picks the best-fitting free bay within opening hours, or any bay free at that time; with none
  free it is 409 `NO_BAY_AVAILABLE`. An edit keeps the car on its bay while that still works.
- Two open bookings can never overlap on one bay, even when requests race: the database refuses it and the API
  answers 409 `BAY_TAKEN`. Cancelling frees the bay.

Free times (`from`/`to` are local days, at most 14; times in responses are UTC):
- `GET /api/availability?from=2026-11-16&to=2026-11-20&serviceIds=..&serviceIds=..&vehicleSize=Large` (staff):
  `durationMinutes` and `slots` of `{ start, end, suggestedBayId, suggestedBayName, freeBays }` from now on.
- `GET /api/public/studios/{studioId}/availability?...` (no login, published studios, rate-limited): the same `slots`
  as `{ start, end }` only, after the studio's lead time. Unknown or another studio's service: `SERVICE_NOT_FOUND`.
- `GET /api/schedule/day?date=2026-11-16`: per bay its bookings, free gaps and `utilisationPercent` (booked share of
  the open time), the studio's overall utilisation, `unassigned` bookings (no bay) and the day's pending requests.

How the planner chooses: per bay, free time is the opening hours minus its bookings widened by the buffer; every grid
start where the whole visit fits is a candidate. At each start the **best-fit** bay wins: the one the visit leaves the
least unusable gap on (a gap shorter than the studio's quickest service on its smallest car), then the least leftover
time, so long free stretches stay whole for long jobs. Around a preferred time (the request plan) slots are ranked by
minutes away from it plus half a minute per wasted minute.

## Inventory: stock ledger and what visits use

- Stock only changes through movements: `Initial`, `Purchase`, `Adjustment` (stocktake), `Consumption` and
  `Correction`. An item's `stock` is their sum and may go below zero when visits used more than was recorded.
  - `POST /api/inventory/{id}/movements` `{ "kind": "Purchase", "quantity": 5, "note": "Faktura 12/11" }` adds a
    delivery; `{ "kind": "Stocktake", "quantity": 4.5 }` sets the counted stock. Quantities have at most three decimals.
  - `GET /api/inventory/{id}/movements` (paged, newest first) is the item's history.
  - `PUT /api/inventory/{id}` with a different `stock` is recorded as a stocktake.
- What services use:
  - `GET/PUT /api/inventory/recipes/{serviceId}` `{ "lines": [{ "itemId": "..", "quantityForMedium": 0.2 }] }` (owner
    writes): the materials of one service on a Medium car.
  - `GET/PUT /api/inventory/size-factors` `{ "small": 0.75, "medium": 1, "large": 1.3, "xl": 1.6 }`: how much more material
    each size takes. They are separate from the time multipliers.
- When a booking is completed, Scheduling publishes `BookingCompleted` and Inventory takes the estimate off stock:
  norm × size factor × what the studio has learned. It happens once per booking, a moment after the request (on the bus).
  - `GET /api/inventory/consumptions/{bookingId}` shows per service and material the `baseQuantity` (norm for the size),
    `estimatedQuantity` (deducted) and `actualQuantity`. It is 404 `CONSUMPTION_NOT_FOUND` until recorded.
  - `PUT /api/inventory/consumptions/{bookingId}` `{ "lines": [{ "serviceId", "itemId", "actualQuantity": 0.3 }] }`
    records what the visit really used. Stock moves by the difference. The first actual amount of each line teaches
    the estimate (exponential smoothing, applied after three visits); later edits only fix stock.
  - `GET /api/inventory/consumptions/accuracy`: per service and material the learned `factor`, `samples` and the mean
    absolute percentage error of the corrected estimates.
- `GET /api/inventory/forecast`: per item its `averageDailyUse` (last 30 days), `daysLeft`, `plannedUse` by the open
  bookings of the next two weeks, `stockAfterPlanned` and `runsOutAt` (the start of the first booked visit stock won't
  cover).
