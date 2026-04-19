---
name: portabox-design
description: Use this skill to generate well-branded interfaces and assets for PortaBox, either for production or throwaway prototypes/mocks/etc. Contains essential design guidelines, colors, type, fonts, assets, and UI kit components for prototyping.
user-invocable: true
---

Read the README.md file within this skill, and explore the other available files.
If creating visual artifacts (slides, mocks, throwaway prototypes, etc), copy assets out and create static HTML files for the user to view. If working on production code, you can copy assets and read the rules here to become an expert in designing with this brand.
If the user invokes this skill without any other guidance, ask them what they want to build or design, ask some questions, and act as an expert designer who outputs HTML artifacts _or_ production code, depending on the need.

## Quick orientation
- **PortaBox** is a multi-tenant SaaS for package control in Brazilian condominiums (pt-BR). Tagline: *Logística Interna Simplificada*.
- Three audiences: **porteiro** (mobile Capture app), **morador** (WhatsApp/SMS/email messages — no app), **síndico** (web dashboard).
- Palette is navy (trust) + orange (action). No gradients, no illustrated packages, no extra accents.
- Copy is pt-BR, sentence case, "você", warm but utility-first. `📦` emoji is sanctioned *only* in resident-facing WhatsApp/SMS.
- Tokens live in `colors_and_type.css`. Logo in `assets/logo-portabox.png` / `assets/logo-portabox-mark.svg`. Icon system = Lucide.
- UI kits: `ui_kits/porter-app/`, `ui_kits/resident-message/`, `ui_kits/admin-dashboard/`.