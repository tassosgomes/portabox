# UI Kit — Resident Notifications

The resident's side of PortaBox. **No app to install** — residents receive the
arrival notice through the condo's existing messaging channel:

- **WhatsApp** (primary for most Brazilian condos)
- **SMS** (fallback)
- **Email** (secondary)

The message always carries the **4-digit PIN** (Token de Retirada). Residents
read the PIN to the porteiro at the counter.

## Components
- `PhoneChrome` — iPhone-style frame showing the message in context
- `WhatsAppMessage` — the arrival bubble (standard WhatsApp styling)
- `SMSMessage` — iOS-style SMS bubble
- `EmailMessage` — minimalist email preview
