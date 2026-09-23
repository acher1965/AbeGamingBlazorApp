# AbeGaming - Board Game Tools

A Progressive Web App (PWA) built with Blazor WebAssembly to provide helpful tools for board gaming enthusiasts.

🌐 **Live Site:** [abegaming.org](https://abegaming.org)

## Features

Both calculators are currently in **BETA**: feedback is welcome (see the Contact page in the app).
Each one can roll a single battle result, or compute the probabilities of victory, the
distribution of losses and other expected values for a given battle setup.

### For The People Battle Calculator
A battle resolution calculator for GMT Games' acclaimed American Civil War card-driven strategy game "For The People". 

The calculator handles:
- Land battle resolution with full Combat Results Table (CRT) implementation
- Die roll modifications (DRM) for leaders, elites, fortifications, and supply status
- Amphibious assaults
- Leader casualty checks
- Army size ratios and battle sizes
- Post-battle movement options
- Exact statistics, cross-checked against a Monte Carlo simulation in the test suite

### Paths of Glory Battle Calculator
The same kind of tool for GMT Games' World War I card-driven strategy game "Paths of Glory".

The calculator handles:
- Corps and Army fire tables, with factors and DRM for each side
- Terrain (clear, forest, marsh, mountain, desert) and trench column shifts
- Fortresses
- Flank attacks, including the extra-attacking-space DRM
- Out-of-supply units and the Sinai attacker penalty

## Technology Stack

- **Framework:** Blazor WebAssembly (.NET 10)
- **Hosting:** Cloudflare Pages
- **Features:** Progressive Web App (PWA) with offline support
- **CI/CD:** Automated builds via Cloudflare Pages

## Installation as PWA

This app can be installed on your device for offline use:

### Mobile (iOS/Android)
- **iOS Safari:** Tap Share → "Add to Home Screen"
- **Android Chrome:** Tap Menu (⋮) → "Install app"

### Desktop
- Look for the install icon in your browser's address bar
- Or use browser menu → "Install AbeGamingBlazorApp"

## Development

### Prerequisites
- .NET 10 SDK (stable; `global.json` excludes preview SDKs)
- Git

### Local Setup
```bash
git clone https://github.com/acher1965/AbeGamingBlazorApp.git
cd AbeGamingBlazorApp
dotnet restore
dotnet run --project AbeGamingBlazorApp
```

The solution file is `AbeGamingBlazorApp.slnx` (the newer XML solution format, not a classic `.sln`).

### Running Tests
```bash
dotnet test AbeGaming.GameLogic.Tests/AbeGaming.GameLogic.Tests.csproj
dotnet test AbeGaming.BlazorApp.Component.Tests/AbeGaming.BlazorApp.Component.Tests.csproj
```

The Playwright browser tests need a running app and a one-off browser install; see
[AbeGaming.BlazorApp.E2E.Tests/README.md](AbeGaming.BlazorApp.E2E.Tests/README.md).
CI (GitHub Actions) runs the two suites above on every push and pull request to
`develop` and `master`.

### Building for Production
The repository includes a `build.sh` script for Cloudflare Pages deployment that:
- Installs .NET 10 SDK
- Installs `wasm-tools` workload for optimized WebAssembly output
- Generates changelog from git commits
- Publishes the application
- Configures SPA routing for Cloudflare Pages

## Project Structure

```
AbeGamingBlazorApp.slnx
├── AbeGaming.GameLogic/                 # Game rules engine (no UI dependencies)
│   ├── FtP/                             # For The People: CRT, battle model, exact stats, Monte Carlo
│   ├── PoG/                             # Paths of Glory: CRTs, battle model, exact stats
│   └── Dice.cs, HitStats.cs, ...        # Shared helpers
├── AbeGamingBlazorApp/                  # Blazor WebAssembly PWA
│   ├── Components/                      # Reusable UI parts (side inputs, stats displays, InfoTip)
│   ├── Pages/                           # Routable pages
│   │   ├── Home.razor                   # Landing page
│   │   ├── FtpBattlePage.razor          # For The People calculator
│   │   ├── PoGBattle.razor              # Paths of Glory calculator
│   │   ├── About.razor, Contact.razor   # Info pages
│   │   ├── Install.razor                # PWA install instructions
│   │   └── ChangeList.razor             # Git commit history
│   ├── Layout/                          # App layout and navigation menu
│   └── wwwroot/                         # Static assets, service worker, manifest
├── AbeGaming.GameLogic.Tests/           # xUnit tests for the rules engine
├── AbeGaming.BlazorApp.Component.Tests/ # bUnit component tests
└── AbeGaming.BlazorApp.E2E.Tests/       # Playwright browser tests
```

`RulesAndTables/` holds the reference rules and charts used to implement the calculators.

## Possible Future Work

### Usage counting per calculation
Visitor numbers come from Cloudflare Web Analytics, which counts page visits but not how many
battles are actually calculated. A possible extension:
- Add a small Cloudflare Pages Function endpoint (e.g. `/api/usage`) that records one event per
  "Calculate Exact Stats", "Roll 1 Battle" or Monte Carlo run, with the calculator name (FtP/PoG)
  and action type only, no personal data.
- Store the events in Workers Analytics Engine (or D1) and query the totals from the Cloudflare dashboard.
- Send the events fire-and-forget from the Blazor app, so a failed call never affects the calculators.
- Optionally use Cloudflare Turnstile in invisible mode to count only verified humans.
- Limitation: usage of the installed PWA while offline cannot be counted.

## Contributing

This is a personal hobby project, but suggestions and feedback are welcome! Feel free to:
- Open an issue for bug reports or feature requests
- Fork the repository and submit pull requests

### Versioning

This project uses [Semantic Versioning](https://semver.org/):
- **MAJOR** (x.0.0): Breaking changes or major rewrites
- **MINOR** (0.x.0): New features (e.g., new game calculator)
- **PATCH** (0.0.x): Bug fixes, small improvements

**To release a new version:**

1. Update the version in `AbeGamingBlazorApp/AbeGamingBlazorApp.csproj`:
   ```xml
   <Version>1.1.0</Version>
   <AssemblyVersion>1.1.0</AssemblyVersion>
   <FileVersion>1.1.0</FileVersion>
   ```
2. Commit and push to `develop` branch
3. Create a PR from `develop` → `master`
4. When merged, a git tag `v1.1.0` is automatically created

The version is displayed in the app's navigation menu.

## Useful Links

- [GMT Games](https://www.gmtgames.com/) - Publisher of "For The People" and "Paths of Glory"
- [For The People on BoardGameGeek](https://boardgamegeek.com/boardgame/833/for-the-people)
- [Paths of Glory on BoardGameGeek](https://boardgamegeek.com/boardgame/91/paths-of-glory)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)

## License

This project is provided as-is for educational and personal use. 

"For The People" and "Paths of Glory" are trademarks of GMT Games LLC. These tools are unofficial fan-made calculators and are not affiliated with or endorsed by GMT Games.

## Changelog

Recent changes can be viewed on the [Change List](https://abegaming.org/changelist) page, which automatically updates from git commits.

---

**Made with ☕ and 🎲 by a board gaming enthusiast**