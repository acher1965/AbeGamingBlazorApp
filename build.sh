#!/usr/bin/env bash
set -e 

# --- CONFIGURATION ---
PROJECT_FOLDER="AbeGamingBlazorApp" 
# ---------------------

echo "Installing .NET 10.0..."
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0 --install-dir ./dotnet-sdk

# Install wasm-tools workload for optimized WebAssembly output
echo "Installing wasm-tools workload..."
./dotnet-sdk/dotnet workload install wasm-tools --skip-manifest-update

# --- GENERATE CHANGELOG FROM GIT COMMITS ---
echo "Generating changelog from git commits..."
# Use a simple approach that handles special characters in commit messages
# The || true ensures build continues even if changelog generation fails
(
  echo "["
  first=true
  git log --format="%H|%h|%ci|%an|%s" HEAD | while IFS='|' read -r hash short date author message; do
    # Escape quotes and backslashes in message for valid JSON
    message=$(echo "$message" | sed 's/\\/\\\\/g' | sed 's/"/\\"/g')
    author=$(echo "$author" | sed 's/\\/\\\\/g' | sed 's/"/\\"/g')
    if [ "$first" = true ]; then
      first=false
    else
      echo ","
    fi
    printf '  {"hash": "%s", "shortHash": "%s", "date": "%s", "author": "%s", "message": "%s"}' "$hash" "$short" "$date" "$author" "$message"
  done
  echo ""
  echo "]"
) > "$PROJECT_FOLDER/wwwroot/changelog.json" 2>/dev/null || echo "[]" > "$PROJECT_FOLDER/wwwroot/changelog.json"
echo "Changelog generated."

echo "Building $PROJECT_FOLDER..."
./dotnet-sdk/dotnet publish "$PROJECT_FOLDER" -c Release -o ./dist

# --- SPA ROUTING FIX FOR CLOUDFLARE PAGES ---
# Copy index.html to 404.html - Cloudflare serves 404.html for unknown routes,
# allowing Blazor's client-side router to handle the navigation
cp ./dist/wwwroot/index.html ./dist/wwwroot/404.html

# Note: _redirects with "/* /index.html 200" causes Cloudflare to warn about infinite loop
# but we keep it commented here in case we need to re-enable it
# echo "/* /index.html 200" > ./dist/wwwroot/_redirects

echo "Build and SPA routing configuration complete."
echo "Output directory contents:"
ls -la ./dist/wwwroot/