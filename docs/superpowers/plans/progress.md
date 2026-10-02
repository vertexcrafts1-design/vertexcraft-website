# VertexCraft rebuild progress

Baseline: main site ae89f1a; shop 841fab9. Existing API and plugin configuration inspected. No AGENTS.md found. Work isolated on codex/vertexcraft-rebuild-20261002 in both checkouts.
Stripe connected. Payment Link prices cannot be edited in place (update only supports line-item quantities); new prices and links require an updated Minecraft identity mapping before activation.
Meaningful shop tests written; browser installation required before baseline run.

## Implementation and checks
- Rebuilt 22 main-site HTML pages and 4 shop pages, semantic shared styles, preserved domains and legal operator content.
- Preserved current 11 Stripe URLs/link identities and fulfillment mappings. Six fair lower prices created in Stripe inactive; IDs recorded in shop/integration/stripe-catalog.json. No fair server claim before installation.
- Checkout baseline contract tests exposed Bedrock name rejection and forged return confirmation. Fixed; 6/6 contract tests pass. Worker baseline fixtures exposed missing async payment-status gate and invalid-name passthrough; prepared worker 3.4.1 fixes, 7/7 pass.
- Static release check passes all 26 pages, assets, local references, anchors, explicit JS IDs and CSP.
- Reviewer identified static fake dashboard events/old season, unknown data presented as 0, mobile menu Escape focus, missing preference removal and nested-404 references. Fixed.
- Local Playwright browser could not install (network downloads incomplete). Cloud browser local loopback denied. Browser UI inspection deferred to deployed existing domains; no UI-test passing claim yet.
- Public Worker request in this environment returns Cloudflare 403/error1010. Do not infer the server is down. No live auth, real charge or Minecraft fulfillment test performed.
- Current plugin source and server/Cloudflare installation access absent. Fair plugin rebuild + new Payment Links and mapping activation remain blocked; concrete migration packet prepared.

## Published result
- Main release c55d36bb5a2e8a6c57cef9a036d7ff7f541d0b4e; shop release 17cbe3b9d8da455480fe7cd79d205cb3c7893509. Both GitHub Pages runs succeeded.
- All 26 live pages return HTTP200 and match released local HTML exactly.
- Live browser observed main desktop layout, Java/Bedrock join instructions, shop duration price changes, category switch, forged payment-return message, native player dialog and Escape focus returning to trigger. No real charge.
- Live .Gamefly lookup reaches friendly unavailable state; cannot establish upstream availability from this browser. Store/API failure behavior remains closed.
- Actual handler harness: missing player, offline API, wrong canonical name, valid Bedrock recipient, canceled pending checkout and forgotten recipient all pass.
- New scoped dashboard focus styles, menu aria/expanded/Escape and honest loaded-count labels applied.
- Live authentication, real fulfillment/deduplication, fair plugin installation and phone/200% zoom layout remain unverified; do not claim complete fair rollout.
