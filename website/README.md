# Gem Rush 3D — Official Website

The official marketing site for **Gem Rush 3D**, a cozy 3D platformer for
Android and Windows. Hand-coded static HTML/CSS/JS — no frameworks, no build
step, no tracking — following the game's own zero-asset spirit and its
"Evergreen Law" (no level counts or totals anywhere in the copy, so the site
never goes stale as the realm grows).

## Files

```
website/
├── index.html          Landing page (hero, features, story, realms, sound,
│                       gallery, download, FAQ)
├── presskit.html       Press kit (factsheet, descriptions, assets)
├── 404.html            "The ink froze mid-word" error page
├── sitemap.xml         Search-engine sitemap
├── robots.txt          Crawler rules + sitemap pointer
├── site.webmanifest    PWA metadata (name, icons, theme color)
├── assets/
│   ├── css/style.css   The whole design system (palette = game's ArtLib)
│   ├── js/main.js      Nav, lightbox, reveal-on-scroll, live version badge
│   └── img/            og-image, icons (from AppIcon.png), screenshots
└── tools/generate_assets.py   Regenerates all derived images (needs Pillow)
```

## Editing

- **Colors/typography**: all tokens live at the top of `assets/css/style.css`.
  They mirror `docs/Art-Direction.md` — keep them in sync with the game.
- **Download links**: point at
  `https://github.com/jaszyxt/GemRush3D/releases/latest`. When the game ships
  on a store, replace the matching button `href` in `index.html` (hero +
  download card) and flip the store chip from "Coming soon" to a real link.
- **Version badge**: `assets/js/main.js` fetches the latest release tag from
  the GitHub API at view time; the hardcoded `v1.12.1` in the HTML is only the
  no-network fallback.
- **New screenshots**: drop renders into `Assets/Screenshots/`, add a mapping
  in `tools/generate_assets.py`, run it, then add a `<figure>` to the gallery.
- **Canonical/OG URLs**: pages use relative links, but the absolute URLs in
  each `<head>` (canonical, `og:image`, `sitemap.xml`, `robots.txt`) assume the
  site is served at `https://jaszyxt.github.io/GemRush3D/`. If you get a real
  domain, search for `jaszyxt.github.io` and replace.

## Deploy (GitHub Pages)

1. Repo **Settings → Pages**.
2. Source: **GitHub Actions**.
3. Add `.github/workflows/pages.yml`:

```yaml
name: Deploy website
on:
  push:
    branches: [main]
    paths: ["website/**"]
permissions:
  contents: read
  pages: write
  id-token: write
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/configure-pages@v5
      - uses: actions/upload-pages-artifact@v3
        with:
          path: website
      - id: deployment
        uses: actions/deploy-pages@v4
```

The site then lives at `https://jaszyxt.github.io/GemRush3D/`. Netlify /
Cloudflare Pages / itch.io embeds work too — it is a plain static folder;
just publish the contents of `website/`.

## Regenerating image assets

```
python -m pip install pillow
python website/tools/generate_assets.py
```

Copies the curated screenshots from `Assets/Screenshots/`, derives the favicon
set from `Assets/Textures/AppIcon.png`, and composes the 1200×630 social card.

## Agile log

| Sprint | Scope | Outcome |
|--------|-------|---------|
| 0 | Foundation | Folder scaffold, asset pipeline, favicon set, og-image |
| 1 | Landing page | `index.html` + full CSS design system (ArtLib palette) |
| 2 | Content pages | `presskit.html`, `404.html` (level catalog dropped — evergreen scope) |
| 3 | SEO | JSON-LD `VideoGame`, OG/Twitter meta, sitemap, robots, manifest |
| 4 | Interaction | Mobile nav, lightbox, reveal animations, live release badge |
| 5 | QA | Browser-verified at 1280/375 px; fixes: unusable screenshots dropped, press-kit mobile nav, no-JS reveal gating, 404 fonts; link/asset/JSON-LD validation script pass |

Evergreen rule for future edits: **no counts** (levels, packs, stars). Speak
of realms, weather and the growing atlas instead — new content never breaks
the copy.
