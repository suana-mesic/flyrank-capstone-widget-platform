# BUILDLOG — AI-usage log

Honesty is graded, not perfection. This log records where AI helped, where it was
wrong, and what I changed.

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

- The honeypot first returned 400 when the hidden `website` field was filled. That tells a bot
  the field is a trap, so it just stops filling it. I changed it to return a fake 201 and silently
  drop the row instead — the bot sees success and never learns the honeypot exists.
- The AI first suggested a global `AllowAnyOrigin()` CORS policy. I rejected it: that would let any
  site call the authenticated dashboard routes too. I scoped a named `"public"` policy to only
  `/config` and `/submissions`, and left the admin/dashboard routes with no CORS at all.

## What I verified myself

- Ran the two-tenant isolation check by hand (tenant B → 404, dashboard `total: 0`).
- Confirmed the rate limiter with a burst (`201 201 201 201 201 429 429`).
- Took each dependency down on purpose (`/debug/geo-primary`, `/debug/webhook-switch`) to prove
  the submission still succeeds when a secondary service fails.

## Cost & grounding (if AI used for content)

- No paid AI is used in the core build. Captions/content are not part of this capstone.
