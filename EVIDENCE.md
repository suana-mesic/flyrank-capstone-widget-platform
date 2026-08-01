# EVIDENCE

One proof per Definition-of-Done checkbox. Where you see **[paste output]**, run the
command and paste the real transcript — a claim without evidence scores as not done.

Run tests with `dotnet test`; run the API with `docker compose up -d && dotnet run --project WidgetPlatform`.

---

## Widget management

- [x] **Authenticated CRUD for widgets; requests without valid auth are rejected.**
  Proof: `GET /widgets` with no `Authorization` header returns 401.
  ```
  [paste: curl -i http://localhost:5133/widgets]
  ```

- [x] **Multi-tenant isolation proven (tenant A cannot read/modify tenant B's widgets or submissions).**
  Proof: repositories require `tenantId` (`GetByIdForTenantAsync(id, tenantId)`); tenant B asking for tenant A's widget gets 404, and B's dashboard reports `total: 0`.
  ```
  [paste: two-tenant curl transcript showing 404 + total:0]
  ```

- [x] **Embed snippet generated per widget.**
  Proof: `GET /widgets/{id}/embed` returns the `<script>` snippet.
  ```
  [paste: curl http://localhost:5133/widgets/1/embed with a valid token]
  ```

## Widget delivery

- [x] **Public config endpoint serves a small payload with correct HTTP cache headers.**
  Proof: `GET /widgets/{id}/config` returns `Cache-Control: public, max-age=300` and an `ETag`; a second request with `If-None-Match` returns 304.
  ```
  [paste: curl -i http://localhost:5133/widgets/1/config  (then repeat with If-None-Match)]
  ```

- [x] **Widget JS served as a versioned bundle (new version = new URL / cache-bust).**
  Proof: `GET /widget.js` returns `Cache-Control: public, max-age=3600`; the snippet points at `widget.js?v=1`.
  ```
  [paste: curl -i http://localhost:5133/widget.js]
  ```

- [x] **Widget renders on a page served from a different origin.**
  Proof: `customer-site/index.html` opened as `file://` (a different origin from `localhost:5133`) renders the widget and submits across the boundary.
  ```
  [paste: screenshot filename, e.g. proof-second-origin.png]
  ```

## Public submission API

- [x] **Cross-origin submissions work; CORS + preflight (OPTIONS) handled.**
  Proof: test `CorsTests` — preflight from any origin is allowed on `/submissions`, and admin routes get no CORS headers.
  ```
  [paste: dotnet test output for CorsTests]
  ```

- [x] **All input validated; malformed/oversized payloads rejected with 4xx + JSON errors.**
  Proof: tests `SubmissionValidationTests` — unknown field → 400, oversized value → 400, empty payload → 400.
  ```
  [paste: dotnet test output for SubmissionValidationTests]
  ```

- [x] **Valid submissions stored, linked to the right widget and tenant.**
  Proof: a valid POST returns 201 and the row appears under the owner's dashboard.
  ```
  [paste: curl POST /submissions returning 201, then GET /dashboard/submissions]
  ```

## Abuse protection

- [x] **Rate limiting per IP / per widget returns 429 under a burst; the API keeps serving legit traffic.**
  Proof: test `RateLimitTests` — a burst of 7 gives `201 201 201 201 201 429 429`.
  ```
  [paste: dotnet test output for RateLimitTests]
  ```

- [x] **At least one spam-prevention technique demonstrably blocks a spam submission.**
  Proof: hidden honeypot `website` field — if non-empty, the request gets a fake 201 and nothing is stored.
  ```
  [paste: curl POST /submissions with a non-empty "website" field, then show it is not in the dashboard]
  ```

## Enrichment & safe side effects

- [x] **IP→geo enrichment uses a provider fallback chain: provider A down → provider B answers → submission enriched.**
  Proof: test `GeoEnricherTests` — primary up → Sarajevo; `POST /debug/geo-primary?up=false` → Mostar.
  ```
  [paste: dotnet test output for GeoEnricherTests + curl with debug switch]
  ```

- [x] **All providers down → submission still succeeds (without geo). Degrade, never fail.**
  Proof: test `Returns_null_when_every_provider_fails` — the submission is still stored with no geo.
  ```
  [paste: dotnet test output]
  ```

- [x] **A failing confirmation email / webhook does not prevent the submission from being stored.**
  Proof: `POST /debug/webhook-switch?up=false` → the log shows the webhook failing, the visitor still gets success, and the row is in the database.
  ```
  [paste: curl POST /submissions after taking the webhook down, showing 201 + stored row]
  ```

## Tests & documentation

- [x] **Automated tests cover: CORS preflight, invalid payload, oversized payload, rate limiting, spam control, provider fallback, successful widget rendering.**
  Proof: `dotnet test` — 10 tests green (CorsTests, SubmissionValidationTests, RateLimitTests, GeoEnricherTests).
  ```
  [paste: full dotnet test summary showing 10 passed]
  ```

- [x] **README with architecture diagram, setup instructions, and API documentation; the five submission-pack files present.**
  Proof: `README.md` + `architecture-diagram.png`, plus `capstone.yaml`, `EVIDENCE.md`, `BUILDLOG.md`, `.env.example`.
