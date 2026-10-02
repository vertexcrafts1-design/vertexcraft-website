# VertexCraft rebuild implementation plan

> Implement in this session with executing-plans; retain existing payment and authentication boundaries.

**Goal:** Rebuild the existing main site and shop with accurate Season 2 content, a clear purchase flow and affordable ranks.

**Architecture:** Two static GitHub Pages repositories keep their existing domains. Shared visual tokens and semantic HTML support phone and desktop use. Public player API, protected dashboard and signed Stripe fulfillment stay on the existing Worker.

**Tech stack:** HTML, CSS, vanilla JavaScript; Python local server; Playwright verification; Stripe Payment Links.

**Spec:** ../specs/2026-10-02-vertexcraft-design.md

## Global constraints

- Preserve domains, Worker routes, payment-link IDs, fulfillment commands and existing operator information.
- German, factual community copy; no fabricated rewards, events, statuses or popularity.
- Site/checkout prices agree. Never claim paid gameplay advantages have disappeared before server rollout.
- Player names are purchase attribution; payment confirmation comes from signed server events.

## Review focus

- A forged success query cannot claim a successful payment.
- Stored player names are revalidated before checkout; failure never opens payment.
- Bedrock player names, API failure and server-provided invalid names fail safely.
- Protected dashboard script runs under CSP and unauthorized views remain protected.
- At 390px and 200% text zoom, navigation, product prices and comparison tables remain usable.

### Task 1: Main site and subpages

Files: `vertex.css`, `script.js`, all public HTML, `stats.js`; new join/game/PvP/vote/help pages. Use existing assets and source API.

- [ ] Build shared navigation and complete public pages from the spec.
- [ ] Preserve legal body and actual dashboard endpoints; restyle dashboard and extract its inline script.
- [ ] Verify local links, syntax, representative layouts and honest failure states.

### Task 2: Shop and payment safety

Files: shop `index.html`, `vertex-shop.css`, `shop-v3.js`, `catalog.js`, tests.

- [ ] Write and run failing tests for forged success, Bedrock names and revalidation before checkout.
- [ ] Build rank comparison, duration choice, full product information, accessible player dialog and payment review.
- [ ] Keep legacy payment-link identities. Read current Stripe records before changing prices; prepare new price/link rollout without losing plugin mapping.
- [ ] Verify tests, site/checkout price agreement and API errors. No real charge.

### Task 3: Delivery

Files: release notes and server migration documentation.

- [ ] Verify webhook and plugin identity mappings from current available source; explicitly distinguish local verification from live server verification.
- [ ] Review completed changes and fix important defects, then publish compatible site changes to the original repositories.
- [ ] Prepare any server-side changes as a concrete package; do not claim installation without access.
