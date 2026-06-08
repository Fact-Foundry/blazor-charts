# Security Policy

## Supported Versions

| Version | Supported |
|---|---|
| Latest (1.x) | ✅ |
| Older versions | ❌ |

Security fixes are applied to the latest release only. We recommend keeping your package reference up to date.

## Scope

FactFoundry.Blazor.Charts is a client-side UI rendering library. It accepts data provided by the host application and renders it as SVG via Razor components. There is no network access, no data persistence, no authentication, and no server-side execution.

**In scope:**
- SVG rendering that could produce output exploitable via XSS (e.g. unsanitized label or tooltip content injected into SVG markup)
- Denial-of-service via malformed or adversarial input data (e.g. inputs that cause unbounded rendering loops or excessive memory use)
- Dependency vulnerabilities (the library has no runtime dependencies, but tooling dependencies in the build pipeline are in scope)

**Out of scope:**
- Vulnerabilities in the host application's data sources
- Issues with the Blazor framework itself — report those to [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore/security)
- Issues with .NET runtime — report those to [dotnet/runtime](https://github.com/dotnet/runtime/security)

## Reporting a Vulnerability

**Do not open a public GitHub issue for security vulnerabilities.**

Please report security vulnerabilities by email to:

**security@factfoundry.io**

Include the following in your report:
- A description of the vulnerability
- Steps to reproduce or a minimal proof-of-concept
- The version of the package affected
- Any potential impact you've identified

You can expect an acknowledgment within **72 hours** and a status update within **7 days**.

If a fix is warranted, we will:
1. Prepare a patch and publish a new NuGet release
2. Credit you in the release notes unless you prefer to remain anonymous
3. Open a public GitHub Security Advisory once the fix is available

## SVG Injection Note

This library renders user-supplied strings (labels, tooltips, titles, legend text) directly into SVG markup. Blazor's Razor engine HTML-encodes string values rendered via `@expression` syntax, which mitigates most XSS risk in standard usage.

However, if your application passes user-controlled data directly to chart parameters without sanitizing it first, you should be aware that SVG supports `<script>` elements and event handler attributes. We recommend sanitizing any externally sourced strings before passing them to chart components, particularly in applications that render content from untrusted sources.
