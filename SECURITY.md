# Security Policy

## Reporting a Vulnerability

If you believe you have found a security vulnerability in **HOLD IT — Game4Health**,
please report it privately. **Do not open a public GitHub issue.**

- **Email:** guenichewalid@gmail.com
- **Subject prefix:** `[security] holdit-game4health — <short summary>`

Please include, where possible:
- A description of the issue and its impact
- Steps to reproduce (proof-of-concept welcome)
- Affected version / commit hash
- Your name and a way to contact you for follow-up

We will:
1. Acknowledge receipt within **3 business days**.
2. Provide an initial assessment within **7 business days**.
3. Keep you informed as we investigate and remediate.
4. Credit you in the release notes if you wish (or keep your report anonymous).

## Supported Versions

This project is pre-1.0 (`0.x.y`). Only the latest commit on `main` receives
security fixes. Pinned releases will be supported once we cut `1.0.0`.

## Scope

In-scope:
- The FastAPI service in `api/`
- Containerization configuration (`docker-compose.yml`, `api/Dockerfile`)
- Repository configuration (workflows, branch protection, secret scanning)

Out-of-scope:
- Vulnerabilities in third-party Unity packages shipped with the game (report
  upstream). The Unity project under `HealthHack/` is provided as-is.
- Vulnerabilities requiring physical access to the patient device or ESP32
  hardware on a trusted LAN.

## Hardening Already In Place

- Secrets are loaded exclusively from `.env` (gitignored). The application
  fails to start if `DATABASE_URL` or `SECRET_KEY` are unset.
- GitHub native **secret scanning + push protection** is enabled.
- **Dependabot** vulnerability alerts and automated security fixes are enabled.
- `main` is protected: pull request review required before merge.
- CORS is closed by default and must be explicitly allow-listed via
  `CORS_ORIGINS` environment variable.

Thank you for helping keep patients safe.
