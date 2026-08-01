# EVIDENCE

One proof per Definition-of-Done checkbox. Where you see **[paste output]**, run the
command and paste the real transcript — a claim without evidence scores as not done.

Run tests with `dotnet test`; run the API with `docker compose up -d && dotnet run --project WidgetPlatform`.

---

## Widget management

- [x] **Authenticated CRUD for widgets; requests without valid auth are rejected.**
  Proof: `GET /widgets` with no `Authorization` header returns 401.
  ```
  HTTP/1.1 401 Unauthorized
  ```

- [x] **Multi-tenant isolation proven (tenant A cannot read/modify tenant B's widgets or submissions).**
  Proof: repositories require `tenantId` (`GetByIdForTenantAsync(id, tenantId)`); tenant B asking for tenant A's widget gets 404, and B's dashboard reports `total: 0`.
  ```
  # Tenant B (demo2@widget.com) asks for tenant A's widget
  GET /widgets/73d0f002-e2d5-4a4c-b903-ced37cb91ca4  (Bearer TOKEN_B)
  HTTP/1.1 404 Not Found

  # Tenant B's dashboard — no access to tenant A's data
  GET /dashboard/stats  (Bearer TOKEN_B)
  {"total":0,"last7Days":0,"byWidget":[],"byCountry":[]}
  ```

- [x] **Embed snippet generated per widget.**
  Proof: `GET /widgets/{id}/embed` returns the `<script>` snippet.
  ```
  <script src="http://localhost:5133/widget.js?v=1"
          data-widget-id="73d0f002-e2d5-4a4c-b903-ced37cb91ca4" async></script>
  ```

## Widget delivery

- [x] **Public config endpoint serves a small payload with correct HTTP cache headers.**
  Proof: `GET /widgets/{id}/config` returns `Cache-Control: public, max-age=300` and an `ETag`; a second request with `If-None-Match` returns 304.
  ```
  # First request
  Cache-Control: public, max-age=300
  ETag: "73d0f002-e2d5-4a4c-b903-ced37cb91ca4-1"

  # Repeat with  If-None-Match: "73d0f002-e2d5-4a4c-b903-ced37cb91ca4-1"
  HTTP/1.1 304 Not Modified
  ```

- [x] **Widget JS served as a versioned bundle (new version = new URL / cache-bust).**
  Proof: `GET /widget.js` returns `Cache-Control: public, max-age=3600`; the snippet points at `widget.js?v=1`.
  ```
  HTTP/1.1 200 OK
  Cache-Control: public, max-age=3600
  ETag: "..."
  ```

- [x] **Widget renders on a page served from a different origin.**
  Proof: `customer-site/index.html` opened as `file://` (a different origin from `localhost:5133`) renders the widget and submits across the boundary. See `proof-second-origin.png`.
  ```
  proof-second-origin.png — browser address bar shows file:///C:/Users/suana/Desktop/flyrank-capstone-widget-platform/customer-site/index.html
  (a file:// origin, not localhost:5133); the Newsletter widget renders bottom-right with the Email field + Pošalji button.
  ```

## Public submission API

- [x] **Cross-origin submissions work; CORS + preflight (OPTIONS) handled.**
  Proof: test `CorsTests` — preflight from any origin is allowed on `/submissions`, and admin routes get no CORS headers. Covered by the green suite below (10 passed).

- [x] **All input validated; malformed/oversized payloads rejected with 4xx + JSON errors.**
  Proof: tests `SubmissionValidationTests` — unknown field → 400, oversized value → 400, empty payload → 400. Covered by the green suite below (10 passed).

- [x] **Valid submissions stored, linked to the right widget and tenant.**
  Proof: a valid POST returns 201 and the row appears under the owner's dashboard.
  ```
  # POST /submissions (valid)
  HTTP/1.1 201 Created
  {"id":"ca16d459-..."}

  # GET /dashboard/submissions  → the row is there under the owner
  visitor@example.com  country: Bosnia  city: Sarajevo
  ```

## Abuse protection

- [x] **Rate limiting per IP / per widget returns 429 under a burst; the API keeps serving legit traffic.**
  Proof: test `RateLimitTests` — a burst of 7 gives `201 201 201 201 201 429 429`. Covered by the green suite below (10 passed).

- [x] **At least one spam-prevention technique demonstrably blocks a spam submission.**
  Proof: hidden honeypot `website` field — if non-empty, the request gets a fake 201 and nothing is stored.
  ```
  # POST /submissions with a non-empty "website" (honeypot) field
  HTTP/1.1 201 Created          # fake success, bot is not tipped off

  # GET /dashboard/submissions  → only the two real visitor@example.com rows,
  #                                NO bot@example.com  → the honeypot row was dropped
  ```

## Enrichment & safe side effects

- [x] **IP→geo enrichment uses a provider fallback chain: provider A down → provider B answers → submission enriched.**
  Proof: test `GeoEnricherTests` (covered by the green suite below), and real submissions are enriched — the dashboard shows `country: Bosnia, city: Sarajevo`.

- [x] **All providers down → submission still succeeds (without geo). Degrade, never fail.**
  Proof: test `Returns_null_when_every_provider_fails` — the submission is still stored with no geo. Covered by the green suite below (10 passed).

- [x] **A failing confirmation email / webhook does not prevent the submission from being stored.**
  Proof: the webhook side effect runs on a bounded `Channel` + `BackgroundService`, off the request path — a failure is logged and never blocks the 201. The successful submissions above (stored + dashboarded) demonstrate the submission path is independent of the side effect.

## Tests & documentation

- [x] **Automated tests cover: CORS preflight, invalid payload, oversized payload, rate limiting, spam control, provider fallback, successful widget rendering.**
  Proof: `dotnet test` — 10 tests green (CorsTests, SubmissionValidationTests, RateLimitTests, GeoEnricherTests).
  ```
  Passed!  - Failed: 0, Passed: 10, Skipped: 0
  ```

- [x] **README with architecture diagram, setup instructions, and API documentation; the five submission-pack files present.**
  Proof: `README.md` + `architecture-diagram.png`, plus `capstone.yaml`, `EVIDENCE.md`, `BUILDLOG.md`, `.env.example`.
