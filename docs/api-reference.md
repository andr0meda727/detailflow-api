# API reference (for the frontend)

Every endpoint, who may call it, what it takes and returns, and its error codes. The cross-cutting rules (errors,
paging, dates and time zones, how planning and the stock ledger work) are in [api-conventions.md](api-conventions.md);
read that first. In Development the live OpenAPI schema is at `/openapi/v1.json` and Scalar at `/scalar`.

## Basics

- **JSON is camelCase**, enums are strings (`"Interior"`, `"Large"`, `"Pending"`), money is złoty as a number with at
  most two decimals (`349.99`), moments are UTC ISO 8601 (`"2026-11-20T08:00:00Z"`), days are `"yyyy-MM-dd"`, times of
  day `"08:00:00"`.
- **Sign-in is Supabase.** Send the session's access token: `Authorization: Bearer <access_token>`. Customers sign up
  with `supabase.auth.signUp`; studio owners register with `POST /api/auth/register` and then sign in with Supabase.
- **Studio context.** Staff endpoints work in one studio. With one membership it is chosen automatically; a member of
  several studios sends `X-Studio-Id: <studioId>` (from `GET /api/studios/my`). Without it: 400 `STUDIO_REQUIRED`;
  a studio the user isn't in: 403 `STUDIO_ACCESS_DENIED`; a token without a profile row: 401 `USER_PROFILE_NOT_FOUND`.
  `/api/me/*`, `/api/public/*`, `/api/studios/my` and invitations never need it.
- **Who may call** (column "Auth" below): `public` = no login; `user` = any signed-in account (no studio);
  `staff` = any member of the current studio; `owner` = the studio's owner (others get 403).
- **Errors** are `application/problem+json` with `status`, `detail` (mostly Polish and fit to show, though a few older ones are English) and `code` (switch on it).
  Validation failures are 400 with `errors: { "<Field>": ["message", ...] }`; keys are the request's property paths
  as in C# (`Name`, `Vehicle.LicensePlate`, `Prices[0].Price`). 429 = rate limit (public and customer writes,
  120 requests/min per IP). 500 has no `code`.
- **Paged lists** take `?page=1&pageSize=20` (max 100) and return `{ items, page, pageSize, totalCount, totalPages }`.

Lookup values the API doesn't list:

| What | Values |
|---|---|
| Roles (`roleId`) | `1` Owner, `2` Manager, `3` Employee (only 2 and 3 can be invited or assigned) |
| Booking status (`statusId`) | `6aeadcad-a836-46fc-a445-3a3a832f49a4` Utworzona (open), `6f4f4496-dd44-4ad8-8021-37ef6b3279dd` Zakończona, `d89cf9d3-ee24-481a-ac76-c5cb224f1c1a` Anulowana |
| Service category | `ExteriorWash`, `Interior`, `Polishing`, `Ceramic`, `Ppf`, `WheelsAndTyres`, `Other` |
| Vehicle size | `Small`, `Medium`, `Large`, `XL` |
| Booking request status | `Pending`, `Accepted`, `Rejected`, `Withdrawn` |
| Stock movement type | `Initial`, `Purchase`, `Adjustment`, `Consumption`, `Correction` |
| Day of week | `Sunday` … `Saturday` |

Bookings return the status by its display `status` name and `statusColor`, not its id; map the name to the id above
when editing.

Shared input rules: phone `^(\+48|0048)?[\s-]?\d{3}[\s-]?\d{3}[\s-]?\d{3}$`; licence plate letters, digits, `-` and
spaces, at most 15; brand and model at most 50; NIP 10 digits with a valid check digit; price 0 to < 1 000 000.

## Accounts, studios and staff (Identity)

| Method and path | Auth | Body → response |
|---|---|---|
| `POST /api/auth/register` | public | `{ email, password, firstName, lastName, studio: { name, phone?, email?, address?, timeZone? } }` → 200 `{ message }`. Password ≥ 8 with a capital and a digit. `EMAIL_TAKEN` |
| `GET /api/studios/my` | user | `[{ studioId, name, role, address, timeZone }]` (`role` = "Owner", "Manager", "Employee") |
| `PUT /api/studios/time-zone` | owner | `{ timeZone: "Europe/Warsaw" }` → 204. `INVALID_TIME_ZONE` |
| `GET /api/employees` | owner | `[{ userId, email, firstName, lastName, roleId, roleName }]` |
| `POST /api/employees/invite` | owner | `{ email, roleId: 2 \| 3 }` → 200 `{ message }`. `SELF_INVITE`, `ALREADY_MEMBER`, `INVITATION_PENDING` |
| `PATCH /api/employees/role` | owner | `{ userId, roleId }` → 200 `{ message }`. `CANNOT_CHANGE_OWNER_ROLE`, `SELF_ROLE_CHANGE`, `INVALID_ROLE` |
| `DELETE /api/employees/{userId}` | owner | 204. `CANNOT_REMOVE_OWNER`, `SELF_REMOVE` |
| `GET /api/invitations/{token}` | public | `{ id, studioName, role, email, expiresAt }`. `INVITATION_NOT_FOUND` |
| `POST /api/invitations/{token}/accept` | user | 200 `{ message }`. `NOT_FOUND`, `EXPIRED`, `ALREADY_USED`, `WRONG_USER` (signed in as another email) |
| `POST /api/invitations/{token}/decline` | user | 200 `{ message }`, same errors |

The invitation email links to `{Frontend:BaseUrl}/accept-invitation?token=<token>`; that page shows the invitation
(GET), makes the person sign in or sign up with the invited email, then accepts. Someone without an account also gets
a Supabase invite email that redirects to the same page.

## Clients and vehicles (CRM)

| Method and path | Auth | Body → response |
|---|---|---|
| `GET /api/clients?search=&page=&pageSize=` | staff | paged `Client`. `search` matches name, email, phone, plate, brand, model |
| `GET /api/clients/{id}` | staff | `Client`. `CLIENT_NOT_FOUND` |
| `POST /api/clients` | staff | `{ name, phone, email?, nip?, vehicle?: { licensePlate, brand?, model?, size? } }` → 201 `Client`. `CLIENT_PHONE_TAKEN` |
| `PUT /api/clients/{id}` | staff | same body (`vehicle` ignored) → `Client` |
| `DELETE /api/clients/{id}` | staff | 204 (soft delete; past bookings keep the copy) |
| `GET /api/vehicle?clientId=&search=&page=` | staff | paged `Vehicle` |
| `POST /api/vehicle` | staff | `{ clientId, licensePlate, brand?, model?, size? }` → 200 `Vehicle` (size defaults to `Medium`) |
| `PUT /api/vehicle/{id}` | staff | same → `Vehicle` (left-out size is kept). `VEHICLE_NOT_FOUND` |
| `DELETE /api/vehicle/{id}` | staff | 204 |

`Client` = `{ id, name, email, phone, nip, vehicles: [Vehicle] }`; `Vehicle` = `{ id, licensePlate, brand, model, size }`.

## Services (Scheduling)

| Method and path | Auth | Body → response |
|---|---|---|
| `GET /api/service` | staff | `[Service]` (active only) |
| `POST /api/service` | staff | `{ name, category?, prices?: [{ vehicleSize, price }], durationMinutes? }` → `Service` |
| `PUT /api/service/{id}` | staff | same; left-out `category`/`prices`/`durationMinutes` are kept, sent `prices` replace all → `Service`. `SERVICE_NOT_FOUND` |
| `DELETE /api/service/{id}` | staff | 204 (deactivates) |

`Service` = `{ id, name, category, prices: [{ vehicleSize, price }], durationMinutes }`. A size with no price is
"price on request"; `durationMinutes` is for a Medium car (default 60). One price per size.

## Bookings

| Method and path | Auth | Body → response |
|---|---|---|
| `GET /api/bookings?isCompleted=&fromDate=&toDate=&page=` | staff | paged `Booking` by `scheduledAt`. Dates are UTC moments, both included; `isCompleted=true` = finished (completed or cancelled). `INVALID_DATE_RANGE` |
| `GET /api/bookings/{id}` | staff | `Booking`. `BOOKING_NOT_FOUND` |
| `POST /api/bookings` | staff | `{ clientId, vehicleId, scheduledAt, statusId, serviceIds, price, notes?, bayId? }` → 201 `Booking`. `BOOKING_CLIENT_OR_VEHICLE_NOT_FOUND`, `NO_BAY_AVAILABLE`, `BAY_TAKEN` (409) |
| `PUT /api/bookings/{id}` | staff | same body → `Booking` |
| `POST /api/bookings/{id}/complete` | staff | `Booking`. `BOOKING_ALREADY_FINISHED` |
| `POST /api/bookings/{id}/cancel` | staff | `Booking`. `BOOKING_ALREADY_FINISHED` |
| `DELETE /api/bookings/{id}` | staff | 204 (deletes) |

`Booking` = `{ id, scheduledAt, endsAt, completedAt, totalPrice, status, statusColor, notes,
vehicle: { id, licensePlate, brand, model, size }, client: { id, name, email, phone }, services: [names], bay: { id, name } | null }`.
`endsAt` is always computed. At most 30 services; `scheduledAt` not in the past, at most 2 years ahead.

## Planning: bays, schedule, free times

| Method and path | Auth | Body → response |
|---|---|---|
| `GET /api/bays` | staff | `[{ id, name, isActive }]` |
| `POST /api/bays`, `PUT /api/bays/{id}` | owner | `{ name }` → bay. `BAY_LIMIT_REACHED` (20), `BAY_NOT_FOUND` |
| `DELETE /api/bays/{id}` | owner | 204. `BAY_HAS_UPCOMING_BOOKINGS` |
| `GET /api/schedule` | staff | `Schedule` |
| `PUT /api/schedule` | owner | `{ openingHours: [{ day, opens, closes }], bufferMinutes, slotStepMinutes, minLeadHours, durationMultipliers?: { small, medium, large, xl } }` → `Schedule`. `OVERLAPPING_OPENING_PERIODS`, `INVALID_OPENING_PERIOD`, `TOO_MANY_OPENING_PERIODS`, `INVALID_PLANNING_RULES` |
| `POST /api/schedule/closures` | owner | `{ date, reason? }` → `Schedule` |
| `DELETE /api/schedule/closures/{date}` | owner | 204. `CLOSURE_NOT_FOUND` |
| `GET /api/schedule/day?date=` | staff | `{ date, planningEnabled, openMinutes, utilisationPercent, bays: [{ bayId, name, bookings, freeGaps: [{ start, end, minutes }], utilisationPercent }], unassigned, pendingRequests: [{ id, preferredAt, customerName, vehicleLicensePlate }] }`; a board booking is `{ id, start, end, clientName, vehicleLicensePlate, status }` |
| `GET /api/availability?from=&to=&serviceIds=..&vehicleSize=` | staff | `{ planningEnabled, durationMinutes, slots: [{ start, end, suggestedBayId, suggestedBayName, freeBays }] }`. At most 14 days, 30 services. `SERVICE_NOT_FOUND` |

`Schedule` = `{ planningEnabled, openingHours, closures: [{ date, reason }], bufferMinutes, slotStepMinutes,
minLeadHours, durationMultipliers }`. Defaults: buffer 15, step 15 (15/30/60), lead 12 h.

## Booking requests (staff side)

| Method and path | Auth | Body → response |
|---|---|---|
| `GET /api/booking-requests?status=&page=` | staff | paged `Request` (pending first, then newest) |
| `GET /api/booking-requests/{id}/plan` | staff | `{ requestId, planningEnabled, durationMinutes, preferred: { start, end, fits, withinOpeningHours, bayId, bayName }, alternatives: [{ start, end, bayId, bayName, minutesFromPreferred, wastedMinutes }], competingRequests: [{ id, preferredAt, customerName }] }` |
| `POST /api/booking-requests/{id}/accept` | staff | `{ scheduledAt?, price?, bayId? }` → `Request` (with `booking`). `BOOKING_REQUEST_ALREADY_DECIDED`, `NO_BAY_AVAILABLE`, `BAY_TAKEN` |
| `POST /api/booking-requests/{id}/reject` | staff | `{ reason? }` (≤ 500) → `Request` |

`Request` = `{ id, studioId, studioName, status, preferredAt, customerName, customerPhone, customerEmail,
vehicle: { licensePlate, brand, model, size }, services: [{ serviceId, name, price }], estimatedPrice,
hasUnpricedServices, notes, rejectionReason, createdAt, decidedAt, booking: { id, scheduledAt, status, totalPrice } | null }`.
Another studio's request: `BOOKING_REQUEST_NOT_FOUND`.

## Inventory

| Method and path | Auth | Body → response |
|---|---|---|
| `GET /api/inventory?isLowStock=&page=` | staff | paged `Item` |
| `GET /api/inventory/{id}` | staff | `Item`. `INVENTORY_ITEM_NOT_FOUND` |
| `POST /api/inventory` | staff | `{ categoryId?, name, description?, unit, stock, minStock? }` → 200 `Item` |
| `PUT /api/inventory/{id}` | staff | same; a changed `stock` is recorded as a stocktake → `Item` |
| `POST /api/inventory/{id}/movements` | staff | `{ kind: "Purchase" \| "Stocktake", quantity, note? }` → `Item` |
| `GET /api/inventory/{id}/movements?page=` | staff | paged `{ id, type, quantity, bookingId, note, createdBy, createdAt }`, newest first |
| `GET /api/inventory-categories` | staff | `[{ id, name }]` |
| `GET/PUT/DELETE /api/inventory-categories/{id}`, `POST /api/inventory-categories` | staff | `{ name }` → category (POST 201). `INVENTORY_CATEGORY_NOT_FOUND` |
| `GET /api/inventory/recipes/{serviceId}` | staff | `{ serviceId, lines: [{ itemId, itemName, unit, quantityForMedium }] }` |
| `PUT /api/inventory/recipes/{serviceId}` | owner | `{ lines: [{ itemId, quantityForMedium }] }` → recipe. `SERVICE_NOT_FOUND` |
| `GET /api/inventory/size-factors`, `PUT` (owner) | staff | `{ small, medium, large, xl }` |
| `GET /api/inventory/consumptions/{bookingId}` | staff | `{ bookingId, vehicleSize, completedAt, lines: [{ serviceId, itemId, itemName, unit, baseQuantity, estimatedQuantity, actualQuantity }] }`. `CONSUMPTION_NOT_FOUND` |
| `PUT /api/inventory/consumptions/{bookingId}` | staff | `{ lines: [{ serviceId, itemId, actualQuantity }] }` → consumption. `CONSUMPTION_LINE_NOT_FOUND` |
| `GET /api/inventory/consumptions/accuracy` | staff | `[{ serviceId, itemId, itemName, factor, factorApplied, samples, meanAbsolutePercentageError }]` |
| `GET /api/inventory/forecast` | staff | `[{ itemId, name, unit, stock, minStock, averageDailyUse, daysLeft, plannedUse, stockAfterPlanned, runsOutAt, isLowStock }]` |

`Item` = `{ id, studioId, categoryName, name, description, unit, stock, minStock }`. Quantities have at most 3 decimals.

## Reports

`POST /api/reports/bookings` (staff) `{ dateFrom, dateTo }` (studio days, both included, at most a year) → 202
`{ message }`; the PDF is emailed to the caller.

## Studio's public profile (owner side)

| Method and path | Auth | Body → response |
|---|---|---|
| `GET /api/marketplace/listing` | staff | `Listing`. `LISTING_NOT_FOUND` before the first save |
| `PUT /api/marketplace/listing` | owner | `{ name, description?, address, city, phone?, latitude, longitude }` → `Listing` (created unpublished) |
| `POST /api/marketplace/listing/publish` / `unpublish` | owner | 204. `LISTING_HAS_NO_SERVICES` |

`Listing` = `{ studioId, name, description, address, city, phone, latitude, longitude, isPublished, categories,
services: [{ id, name, category, prices: [{ vehicleSize, price }] }], averageRating, reviewCount }`.

## Customer map (public)

| Method and path | Auth | Body → response |
|---|---|---|
| `GET /api/public/studios?minLat&maxLat&minLng&maxLng&category=&vehicleSize=&page=` | public | paged `{ studioId, name, city, latitude, longitude, categories, fromPrice, averageRating, reviewCount }` |
| `GET /api/public/studios/{studioId}` | public | `Listing`. `LISTING_NOT_FOUND` |
| `GET /api/public/studios/{studioId}/reviews?page=` | public | paged `{ id, authorName, rating, comment, createdAt }` |
| `GET /api/public/studios/{studioId}/availability?from&to&serviceIds&vehicleSize` | public | `{ planningEnabled, durationMinutes, slots: [{ start, end }] }`. `STUDIO_NOT_FOUND`, `SERVICE_NOT_FOUND` |
| `POST /api/public/studios/{studioId}/estimate` | public | `{ serviceIds, vehicleSize }` → `{ services: [{ serviceId, name, price }], total, hasUnpricedServices }`. `STUDIO_NOT_FOUND`, `BOOKING_REQUEST_UNKNOWN_SERVICES` |

## Customer account (`/api/me`)

| Method and path | Auth | Body → response |
|---|---|---|
| `GET /api/me/profile` | user | `{ name, phone }`. `CUSTOMER_PROFILE_NOT_FOUND` before the first save |
| `PUT /api/me/profile` | user | `{ name, phone }` → profile |
| `GET /api/me/vehicles` | user | `[{ id, licensePlate, brand, model, size }]` |
| `POST /api/me/vehicles` | user | `{ licensePlate, brand?, model?, size }` → 201. `CUSTOMER_GARAGE_FULL` (10), `CUSTOMER_VEHICLE_EXISTS` |
| `PUT /api/me/vehicles/{id}`, `DELETE` | user | same body → car / 204. `CUSTOMER_VEHICLE_NOT_FOUND` |
| `POST /api/me/booking-requests` | user | `{ studioId, serviceIds, vehicleId? \| vehicle?: { licensePlate, brand?, model?, size }, preferredAt, notes? }` → 201 `Request` |
| `GET /api/me/booking-requests?page=` | user | paged `Request`, newest first (the reservation history) |
| `POST /api/me/booking-requests/{id}/withdraw` | user | 204 while pending. `BOOKING_REQUEST_ALREADY_DECIDED` |
| `GET /api/me/reviews/pending` | user | `[{ bookingId, studioId, studioName, completedAt, reviewableUntil }]` |
| `POST /api/me/reviews` | user | `{ bookingId, rating: 1-5, comment? }` → 201 review. `REVIEWABLE_VISIT_NOT_FOUND`, `REVIEW_ALREADY_EXISTS`, `REVIEW_PERIOD_OVER`, `CUSTOMER_PROFILE_REQUIRED` |

Submitting a request: exactly one of `vehicleId` and `vehicle`; at most 20 services; `preferredAt` in the future, at
most a year ahead. Errors: `STUDIO_NOT_FOUND` (not published), `CUSTOMER_PROFILE_REQUIRED`, `CUSTOMER_VEHICLE_NOT_FOUND`,
`BOOKING_REQUEST_UNKNOWN_SERVICES`, `BOOKING_REQUEST_LIMIT` (3 pending per studio), and for studios that plan
`OUTSIDE_OPENING_HOURS` and `BOOKING_REQUEST_TOO_SOON` (use the public availability slots to avoid them).

## Examples

An error, and a validation failure:

```json
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5", "title": "Not Found", "status": 404,
  "detail": "Booking 5d1e… not found", "code": "BOOKING_NOT_FOUND" }

{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "Name": ["Nazwa usługi jest wymagana."], "Prices[0].Price": ["Cena nie może być ujemna."] } }
```

A customer books from the map:

```http
GET /api/public/studios?minLat=50.0&maxLat=50.1&minLng=19.8&maxLng=20.1&vehicleSize=Large
→ { "items": [{ "studioId": "3f2c…", "name": "Detail Kraków", "city": "Kraków", "latitude": 50.06,
     "longitude": 19.94, "categories": ["ExteriorWash", "Interior"], "fromPrice": 89.00,
     "averageRating": 4.7, "reviewCount": 12 }], "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1 }

GET /api/public/studios/3f2c…/availability?from=2026-11-16&to=2026-11-20&serviceIds=9a1e…&vehicleSize=Large
→ { "planningEnabled": true, "durationMinutes": 90,
    "slots": [{ "start": "2026-11-16T07:00:00Z", "end": "2026-11-16T08:30:00Z" }] }

POST /api/me/booking-requests
{ "studioId": "3f2c…", "serviceIds": ["9a1e…"], "vehicleId": "c0de…",
  "preferredAt": "2026-11-16T07:00:00Z", "notes": "Psia sierść na tylnej kanapie" }
→ 201 { "id": "77ab…", "status": "Pending", "estimatedPrice": 89.00, "hasUnpricedServices": false,
        "booking": null, … }
```

Staff answer it:

```http
GET  /api/booking-requests?status=Pending
GET  /api/booking-requests/77ab…/plan       → is the preferred time free, and on which bay
POST /api/booking-requests/77ab…/accept     { "bayId": "b4y1…" }   or {} for the preferred time and best bay
POST /api/booking-requests/77ab…/reject     { "reason": "Brak terminów w tym tygodniu" }
```

A staff booking (status "Utworzona"):

```json
POST /api/bookings
{ "clientId": "…", "vehicleId": "…", "scheduledAt": "2026-11-16T08:00:00Z",
  "statusId": "6aeadcad-a836-46fc-a445-3a3a832f49a4", "serviceIds": ["…"], "price": 249.00 }
```

## Not in the API yet

- Creating a second studio and editing a studio's name, phone or address (master's versions were built on the removed
  Core layer and weren't carried over). The public listing has its own name and address.
- Booking responses carry the status name, not `statusId`.
- Booking-request emails have no link into the frontend yet.
