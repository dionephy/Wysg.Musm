# FIX 2026-01-09 Disable HTTPS redirection for local API

## Summary
Desktop splash login was failing with `SSL connection could not be established` because the API returned `307` redirects from `http://127.0.0.1:5205` to `https://127.0.0.1:5206` (dev cert not trusted). Clients default to HTTP, so the forced redirect broke local sign-in.

## Change
- `apps/Wysg.Musm.Radium.Api/Program.cs`: HTTPS redirection is now gated by config `Http:EnableHttpsRedirect` (default **false**). Local HTTP traffic stays on HTTP and no longer redirects.

## How to re-enable redirect (if you have a trusted cert)
- Set `Http:EnableHttpsRedirect=true` (environment variable `Http__EnableHttpsRedirect=true` or appsettings) when you want HTTP -> HTTPS redirects.

## Validation
- Expect `/api/accounts/ensure` over HTTP to return `200` without a redirect.
- Splash silent restore should connect without SSL errors when API is running on `http://127.0.0.1:5205/`.
