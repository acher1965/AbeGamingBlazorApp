# AbeGaming - Board Game Tools

A Progressive Web App (PWA) built with Blazor WebAssembly to provide helpful tools for board gaming enthusiasts.

🌐 **Live Site:** [abegaming.org](https://abegaming.org)

## Features

### For The People Battle Calculator
A battle resolution calculator for GMT Games' acclaimed American Civil War card-driven strategy game "For The People". 

The calculator handles:
- Land battle resolution with full Combat Results Table (CRT) implementation
- Die roll modifications (DRM) for leaders, elites, fortifications, and supply status
- Leader casualty checks
- Army size ratios and battle sizes
- Post-battle movement options

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
- .NET 10 SDK
- Git

### Local Setup
```bash
git clone https://github.com/acher1965/AbeGamingBlazorApp.git
cd AbeGamingBlazorApp
dotnet restore
dotnet run --project AbeGamingBlazorApp
```

### Building for Production
The repository includes a `build.sh` script for Cloudflare Pages deployment that:
- Installs .NET 10 SDK
- Installs `wasm-tools` workload for optimized WebAssembly output
- Generates changelog from git commits
- Publishes the application
- Configures SPA routing for Cloudflare Pages

## Project Structure

```
AbeGamingBlazorApp/
├── FtpBattle/           # For The People battle calculator logic
│   ├── FtpCRT.cs        # Combat Results Table implementation
│   ├── FtpLandBattle.cs # Battle data model
│   └── FtpBattleMethods.cs # Battle resolution logic
├── Pages/               # Blazor pages
│   ├── FtpBattle.razor  # Battle calculator UI
│   ├── About.razor      # About page
│   ├── ChangeList.razor # Git commit history
│   └── Home.razor       # Landing page
├── Layout/              # App layout components
└── wwwroot/             # Static assets
```

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

- [GMT Games](https://www.gmtgames.com/) - Publisher of "For The People"
- [For The People on BoardGameGeek](https://boardgamegeek.com/boardgame/2829/for-the-people)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)

## License

This project is provided as-is for educational and personal use. 

"For The People" is a trademark of GMT Games LLC. This tool is an unofficial fan-made calculator and is not affiliated with or endorsed by GMT Games.

## Changelog

Recent changes can be viewed on the [Change List](https://abegaming.org/changelist) page, which automatically updates from git commits.

---

**Made with ☕ and 🎲 by a board gaming enthusiast**