# PortaBox — Design System

> **Logística Interna Simplificada**
> Controle inteligente de encomendas para condomínios residenciais.

---

## 1 · Product Context

**PortaBox** is a **multi-tenant SaaS** being built from scratch. It replaces the paper
"caderno de protocolo" (the handwritten log books used at condominium front
desks in Brazil) with an **AI-driven digital workflow**.

### The flow in one sentence
The **porter** (porteiro) batch-photographs package labels at their own pace → an
**AI / OCR pipeline** extracts and validates the data → the **resident** is
notified through the condo's existing messaging channel (WhatsApp / SMS / email)
with a **Retrieval Token (PIN)** — no extra app to install.

### Who uses it

| Persona | Surface | Primary job |
|---|---|---|
| **Porteiro** (Porter / concierge) | Mobile app (PortaBox Capture) | Photograph labels, confirm OCR, hand over packages |
| **Morador** (Resident) | WhatsApp / SMS / email message | Receive arrival notice + PIN, retrieve package |
| **Síndico / Admin** (Condo manager) | Web dashboard (PortaBox Admin) | Monitor throughput, audit deliveries, manage units |

### Why it matters
- Zero-app friction for residents — messaging channels they already use.
- Batch capture respects porters' real rhythm (they're interrupted constantly).
- Audit trail replaces paper logs → fewer disputes, insurance clarity.
- Multi-tenant: one platform serves many condominiums.

---

## 2 · Source Materials

Everything in this design system is derived from the assets the founder provided
at project inception. There is **no prior codebase, no Figma file, and no
component library**. This system is the *first* source of truth.

| Source | Role |
|---|---|
| `assets/logo-portabox.png` | Official logo (raster) — "PortaBox · Logística Interna Simplificada" |
| `assets/brand-palette-reference.png` | Official color palette reference with hex codes + Portuguese role names |
| `assets/img-porter-capture.png` | Porter using the mobile Capture screen (shows OCR + orange CTA) |
| `assets/img-resident-whatsapp.png` | Resident receiving a WhatsApp arrival notice |
| `assets/img-delivery-handoff.png` | Handoff at the front desk (resident shows PIN) |
| `assets/img-porter-desk.png` | Porter lifestyle / workspace context |
| `assets/img-admin-dashboard.png` | Admin tablet view (KPIs + charts on navy background) |

> **Caveat for the reader:** the admin dashboard image is a promotional
> render, not a real product screen. The dashboard UI kit in this system is
> a *first-pass* interpretation — values, layout, and charts should be
> revisited as the real product takes shape.

---

## 3 · Index — What's in this project

```
├── README.md                      ← you are here
├── SKILL.md                       ← skill manifest for cross-project reuse
├── colors_and_type.css            ← CSS variables: colors, type, spacing, shadow
├── assets/                        ← logos, product imagery, reference palette
│   ├── logo-portabox.png          ← raster logo on light paper
│   ├── logo-portabox-mark.svg     ← SVG reconstruction of the box+swoosh mark
│   └── img-*.png                  ← product photography
├── preview/                       ← Design System tab cards
│   ├── type-*.html
│   ├── color-*.html
│   ├── spacing-*.html
│   ├── component-*.html
│   └── brand-*.html
└── ui_kits/
    ├── porter-app/                ← mobile Capture app (React JSX, iOS frame)
    │   ├── index.html
    │   └── components/
    ├── resident-message/          ← WhatsApp / SMS notification mockups
    │   ├── index.html
    │   └── components/
    └── admin-dashboard/           ← desktop web dashboard for síndicos
        ├── index.html
        └── components/
```

---

## 4 · Content Fundamentals

### Language
**Portuguese (Brazil)** is the primary language. All product copy, notifications,
and UI labels are written in pt-BR. English appears only in developer-facing
metadata or when naming technology (e.g. "OCR", "AI").

### Tone
- **Acolhedor mas profissional** — warm but professional. PortaBox sits between
  a neighborhood concierge and a logistics platform.
- **Claro e direto** — direct. Condo staff have seconds to read a screen; residents
  are multitasking when the message arrives.
- **Utilitário, não promocional** — utility-first. Copy explains the *next action*,
  not the *value proposition*, once the user is inside the product.

### Person & voice
- **Second-person, informal "você"** with residents. Brazilian Portuguese default
  — never "tu", never "o(a) senhor(a)" unless legal context requires it.
- **Imperative** for the porter app ("Fotografe a etiqueta", "Confirme os dados"
  — do X, then Y).
- **Third-person institutional** for admin/legal surfaces ("O condomínio
  registrou...").

### Casing
- **Sentence case** for headings, buttons, and most UI (`Nova encomenda`,
  `Fotografar etiqueta`).
- **UPPERCASE with wide tracking** (`0.08em`) only for eyebrows / section labels
  (`ENCOMENDA RECEBIDA`, `TOKEN DE RETIRADA`).
- **Title Case** is avoided in pt-BR — it reads as anglicism.

### Concrete examples (from the brand materials)

| Context | Copy |
|---|---|
| Resident notification (WhatsApp) | `Olá, João! 📦 Chegou uma encomenda para você aqui na portaria. Para retirar, basta informar este PIN ao porteiro: 4829. Te esperamos!` |
| Porter app — capture confirmation | `OCR extração feita por IA` |
| Porter app — primary CTA | `Registrar encomenda` |
| Admin dashboard — KPI label | `Entregues hoje` / `Aguardando retirada` |

### Vocabulary preferences
- **Encomenda** > "pacote" or "caixa" (industry term in Brazil).
- **Morador / moradora** > "usuário" when talking to residents.
- **Portaria** > "recepção" (condo-specific).
- **Retirada** > "entrega" (from the resident's perspective).
- **Síndico** > "administrador" (the legal role in Brazilian condos).
- **PIN / Token de Retirada** — the 4-digit code is the hero of the flow; name
  it consistently.

### Emoji
**Sparingly, only in resident messaging.** The 📦 package emoji is the *one*
emoji sanctioned by the brand — it appears in WhatsApp notifications to make
the message instantly scannable in a crowded chat list. Inside the apps
themselves, emoji are **not used** — icons replace them.

### Numbers, dates, times
- pt-BR formatting: `14/04/2026`, `14h32`, `R$ 1.284,00`.
- Unit numbers are always zero-padded when shown as IDs: `Apto 0701`.

---

## 5 · Visual Foundations

### Color philosophy
PortaBox's palette is **two-pole**: a **deep navy** grounding (trust, logistics,
nighttime condo lobbies) paired with a single **vibrant orange** accent
(movement, the arrow on the box, the CTA). Everything else is neutral —
paper-white surfaces, cool gray dividers, a hint of ice-blue for backgrounds.
No gradients; no secondary accent colors. The orange is **scarce on
purpose** — it only marks *the one thing to do next*.

Exact tokens live in `colors_and_type.css`. See the `color-*` preview cards
for swatches.

### Typography
- **Display / UI** — Plus Jakarta Sans (600–800). Geometric, friendly,
  excellent Portuguese glyph coverage (ç, ã, õ, á).
- **Body** — Inter (400–600). Screen-tuned, reads well at 14–16px.
- **Mono** — JetBrains Mono. Used for **PINs**, package IDs, and OCR raw text.

> ⚠ **Font substitution flag:** the reference materials do not specify font
> files. Plus Jakarta Sans + Inter are Google Fonts chosen as the nearest
> match to the logo's rounded-geometric sans. **Please send the intended
> brand fonts if different**, and we'll swap them in.

### Backgrounds
- **Light paper** (`#F9FAFB`) is the default canvas — echoes the warm
  daylight-lit condo lobby imagery.
- **Deep navy** is used for **chrome surfaces** (app top bar, admin hero,
  marketing hero) — full-bleed, flat, no gradient.
- **Product photography** is warm, natural-light, real condos with real
  people. No stock abstractions, no illustrations of packages. When decorative
  imagery is needed, use real photos from the `assets/` folder.
- **No textures, no patterns, no illustrated backgrounds.** The brand is
  "clean reception desk," not "playful startup."

### Animation
- **Fast and functional.** Default duration 200ms, `cubic-bezier(0.2, 0.7, 0.2, 1)`
  (gentle ease-out).
- **No bounces, no spring physics.** The product is used at work, under
  pressure — whimsy is a distraction.
- **Fades + 4–8px translations** for enter/exit. No sliding full-screen
  transitions.
- **Micro-feedback only** on success moments (a 500ms scale-up on the green
  check when OCR succeeds, and on PIN validation).

### Hover, focus, press
- **Hover** on primary CTA: shift to `--pb-orange-700` (darker burnt
  orange). On secondary/ghost: 8% navy tint overlay.
- **Focus** ring: 3px `rgba(249, 115, 22, 0.35)` halo, always visible
  (accessibility — porters often use phones with gloves/cold hands; admins
  keyboard-navigate).
- **Press**: scale to `0.98`, no color change (the hover color already indicated
  intent).

### Borders
- **1px default**, `--border-default` (`#D1D5DB`).
- **No colored borders** except on focus. **No colored left-border accent
  cards** — these are expressly *not* part of the language.

### Shadows — soft & short
PortaBox uses a small set of soft, navy-tinted shadows that evoke daylight
rather than stage lighting.
- `--sh-xs`, `--sh-sm` for input fields and subtle lifts.
- `--sh-md` for cards on the paper background.
- `--sh-lg` for the resident's WhatsApp-style notification bubble and modal
  dialogs.
- `--sh-inset` for recessed elements (the OCR capture frame).

### Protection gradients vs. capsules
When the navy chrome meets content (e.g. hero image with headline on top),
use a **solid navy card/capsule** behind the text, *not* a gradient overlay.
Gradients feel photographic; capsules feel systematized.

### Layout rules
- **Mobile app**: full-viewport, 16px safe-area padding, 44×44 minimum hit
  targets. Bottom tab bar is 64px tall.
- **Admin dashboard**: 1280–1440 design width, 24px gutters, 12-column grid,
  sidebar 240px.
- **Fixed elements**: app top bar (56px) + bottom tab bar; admin sidebar.
  All other surfaces scroll.

### Transparency & blur
**Used sparingly.** The only blur in the system is the iOS-style status bar on
the phone chrome (`backdrop-filter: blur(12px)` over 80% navy). Interior UI
is all opaque — blur would fight the "clean lobby" feel.

### Corner radii
- **4px** — chips, tags.
- **10px** — inputs, small cards.
- **14px** — standard cards (default).
- **20px** — hero cards, phone frame, OCR capture frame.
- **999px (pill)** — primary buttons, status badges, the CTA in the
  capture-app screenshot.

### Cards — what they look like
```
background:    var(--bg-surface)   /* white */
border-radius: var(--r-lg)         /* 14px */
box-shadow:    var(--sh-md)
border:        none                /* shadow does the work, not a border */
padding:       20–24px
```
Cards **never** combine border + shadow. They never use gradient fills.
Headers inside cards are small (18–22px), sentence case, navy.

### Imagery mood
Warm, natural-light, Brazilian residential. Mid-afternoon sun, cream walls,
brown wood, real uniforms. Never cool, never blueish, never b&w, never high-contrast.
When photos aren't available, use neutral placeholders — **never illustrated
packages, never 3D renders**.

---

## 6 · Iconography

### Approach
PortaBox uses **Lucide** (https://lucide.dev) as its default icon set —
stroke-based, 1.5–2px stroke, 24×24 default grid, rounded joins. Chosen
because:
1. **Stroke style matches the logo** — the PortaBox mark is itself a
   stroke-based line drawing of a box.
2. **Complete coverage** for logistics/package concepts (`package`, `scan`,
   `qr-code`, `bell`, `check-circle`, `shield`, `building-2`, `users`).
3. **CDN-available, tree-shakeable in production.**

> **Substitution flag:** the source materials include no icon system.
> Lucide is our chosen default. If PortaBox later adopts Phosphor, Feather,
> or a custom set, the tokens and component JSX here will need a 1:1 swap.

### Loading (prototypes)
```html
<script src="https://unpkg.com/lucide@latest"></script>
<i data-lucide="package-check"></i>
<script>lucide.createIcons();</script>
```

### Icon usage rules
- **Always paired with a label** in primary navigation and buttons. Icon-only
  controls are reserved for dense toolbars (admin dashboard filters).
- **Stroke width 2px** for active/interactive; **1.5px** for inline body
  ornaments.
- **Color follows text** (`currentColor`). The only icon that gets a
  brand-orange fill is the **package** icon when it's the hero element
  (empty-state illustrations, the OCR success check).
- **Size scale**: 16 (inline), 20 (default UI), 24 (primary nav), 32+
  (marketing / empty states).

### Emoji
**The 📦 emoji is the only sanctioned emoji**, and only in resident-facing
outbound messages (WhatsApp/SMS) — because it renders natively in any chat
app. Inside the product UI, this emoji is replaced by the Lucide `package`
icon.

### Unicode
- `•` (bullet) used as a separator in metadata strips.
- `→` used inline in copy (`Portaria → Morador`). Both are system fonts, not
  icon replacements.

### Logos & assets
- `assets/logo-portabox.png` — full horizontal lockup (logo + wordmark +
  tagline), on paper-colored background.
- `assets/logo-portabox-mark.svg` — SVG reconstruction of the isolated
  box+swoosh mark for use in app icons, favicons, and small-scale UI.
- `assets/brand-palette-reference.png` — the source-of-truth palette card.

---

## 7 · UI Kits

Each product surface has its own kit in `ui_kits/<product>/`.

| Kit | Audience | Format |
|---|---|---|
| `porter-app/` | Porter at the front desk | Mobile (iPhone-frame), pt-BR |
| `resident-message/` | Resident anywhere | WhatsApp / SMS notification mockups in phone chrome |
| `admin-dashboard/` | Síndico / building manager | Desktop web, 1280px |

See each kit's own `README.md` for its component list.

---

## 8 · Open questions & next steps

1. **Font files** — confirm the intended display/body fonts; we've
   substituted Plus Jakarta Sans + Inter.
2. **Icon system** — confirm Lucide or swap.
3. **Admin dashboard real specs** — current kit is illustrative only.
4. **Multi-language** — system is pt-BR today; will Spanish/English be
   needed for expansion?
5. **Empty states / error states** — not yet designed; add once core flows
   are validated with real porters.
