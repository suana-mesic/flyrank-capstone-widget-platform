# BUILDLOG — AI-usage log

Honesty is graded, not perfection. This log records where AI helped, where it was
wrong, and what I changed. Edit the entries below so they match how you actually built it.

---

## Where AI helped

- Scaffolding the multi-tenant repository pattern (`GetByIdForTenantAsync(id, tenantId)`) so
  `tenantId` is a required parameter on every read/write, making cross-tenant leaks hard to write.
- Getting the CORS setup scoped correctly — a named `"public"` policy on `/config` and
  `/submissions` only, with admin routes deliberately left without CORS.
- The geo enrichment fallback chain (`IEnumerable<IGeoProvider>`, try each in order, return null
  if all fail) and the bounded `Channel` + `BackgroundService` for the safe webhook side effect.
- HTTP caching details: `Cache-Control` + `ETag`/`If-None-Match` → 304, and the versioned
  `widget.js?v=1` asset strategy.

## Where AI was wrong / what I changed

- [Fill in: a place where the AI-suggested code didn't compile or didn't behave as expected,
  and what you changed. e.g. a wrong status code, a missing `await`, an ETag that didn't bust,
  a test that passed for the wrong reason.]
- [Fill in: a design suggestion you rejected and why — e.g. a global CORS policy you scoped down,
  or a dependency you decided not to add.]

## What I verified myself

- Ran the two-tenant isolation check by hand (tenant B → 404, dashboard `total: 0`).
- Confirmed the rate limiter with a burst (`201 201 201 201 201 429 429`).
- Took each dependency down on purpose (`/debug/geo-primary`, `/debug/webhook-switch`) to prove
  the submission still succeeds when a secondary service fails.

## Cost & grounding (if AI used for content)

- No paid AI is used in the core build. [If you used a free model for anything, note it here;
  otherwise state that captions/content are not part of this capstone.]
