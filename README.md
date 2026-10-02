# AbeGaming - Board Game Tools

A Progressive Web App (PWA) built with Blazor WebAssembly to provide helpful tools for boardgamers.

🌐 **Live Site:** [abegaming.org](https://abegaming.org)

## Features

Feedback is welcome (see the Contact page in the app). Each calculator can roll a single battle result, or compute the probabilities
of victory, the distribution of losses and other expected values for a given battle setup.
The Napoleonic Wars calculators are currently in **BETA**. 

### For The People Battle Calculator
- Land battle resolution with full Combat Results Table (CRT) implementation
- Die roll modifications (DRM) for leaders, elites, fortifications, and supply status
- Amphibious assaults
- Leader casualty checks
- Army size ratios and battle sizes
- Post-battle movement options
- Exact statistics, cross-checked against a Monte Carlo simulation in the test suite

### Paths of Glory Battle Calculator
- Corps and Army fire tables, with factors and DRM for each side
- Terrain (clear, forest, marsh, mountain, desert) and trench column shifts
- Fortresses
- Flank attacks, including the extra-attacking-space DRM
- Out-of-supply units and the Sinai attacker penalty
- An optional **Detailed units** mode: choose the attacking side, list each side's Armies and Corps
  (full or reduced, with or without a replacement Corps in the Reserve Box) and losses are taken step
  by step as in the rules, including the British and RU CAU loss priority (12.4.5).
  This makes flank-attack return fire, retreats (a full strength attacker must remain) and
  cancelling a retreat exact rather than approximated from the factors. The unit values are in
  `AbeGaming.GameLogic/PoG/PoGUnitTypes.json`. On a phone held in landscape, this mode swaps the
  sidebar for the menu button to leave room for the unit lists.

### The Napoleonic Wars Battle, Naval Battle and Siege Calculators
Three calculators each on its own page, grouped under a single "TNW" entry in the navigation menu and on the Home page.

The Battle calculator (rule 11) handles:
- Each side's dice pool: Units, Commander Battle Rating, nationality bonus, battle event dice
- Terrain crossed by the attacker, failed evasions, a defender that cannot retreat, and defending
  Armies not formed into an Army Group
- Two Rounds, kill/disrupt priority, rout, Overrun, Flag Overrun and the rout Resource roll
- Exact statistics for small and medium battles; larger battles fall back to a Monte Carlo
  estimate, and the result says which one you got

The Naval Battle calculator (rule 13) handles:
- Fleets of any mix of nations, with each nation's dice per Squadron and Squadrons under Refit
- Sinking by "6"s split evenly across nations, and "5"s reducing a Fleet's later dice
- Failed evasions, and battles in an enemy Port or Fortress-Port with shore battery fire
- Exact statistics for small and medium battles, Monte Carlo above that, as for land battles

The Siege calculator (rule 12) handles:
- The Besieging Army's dice pool: Command Rating cap, Commander Battle Rating, nationality bonus,
  and spare Units replacing losses in later Rounds (rule 12.33)
- The Fortress's own strength (2 normally, 4 for Gibraltar) and its Zone modifier (rule 12.32)
- Multi-round Sieges within one Impulse, Overrun, and the "besiegers eliminated" edge case
- Exact statistics (no Monte Carlo needed - the Siege's state space is small and strictly bounded)

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
│   ├── TNW/                             # The Napoleonic Wars: shared dice pool, Battle, Naval, Siege
│   └── Dice.cs, HitStats.cs, ...        # Shared helpers
├── AbeGamingBlazorApp/                  # Blazor WebAssembly PWA
│   ├── Components/                      # Reusable UI parts (side inputs, stats displays, InfoTip)
│   ├── Pages/                           # Routable pages
│   │   ├── Home.razor                   # Landing page
│   │   ├── FtpBattlePage.razor          # For The People calculator
│   │   ├── PoGBattle.razor              # Paths of Glory calculator
│   │   ├── TnwBattlePage.razor          # The Napoleonic Wars: Battle calculator
│   │   ├── TnwNavalPage.razor           # The Napoleonic Wars: Naval Battle calculator
│   │   ├── TnwSiegePage.razor           # The Napoleonic Wars: Siege calculator
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

- [GMT Games](https://www.gmtgames.com/) - Publisher of "For The People", "Paths of Glory", and "The Napoleonic Wars"
- [For The People on BoardGameGeek](https://boardgamegeek.com/boardgame/833/for-the-people)
- [Paths of Glory on BoardGameGeek](https://boardgamegeek.com/boardgame/91/paths-of-glory)
- [The Napoleonic Wars on BoardGameGeek](https://boardgamegeek.com/boardgame/36399/the-napoleonic-wars-second-edition)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)

## License

This project is provided as-is for educational and personal use. 

"For The People", "Paths of Glory", and "The Napoleonic Wars" are trademarks of GMT Games LLC. These tools are unofficial fan-made calculators and are not affiliated with or endorsed by GMT Games.

## Changelog

Recent changes can be viewed on the [Change List](https://abegaming.org/changelist) page, which automatically updates from git commits.

---
