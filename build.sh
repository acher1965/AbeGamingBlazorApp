#!/usr/bin/env bash
set -e 

# --- CONFIGURATION ---
PROJECT_FOLDER="AbeGamingBlazorApp" 
# ---------------------

echo "Installing .NET 10.0..."
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0 --install-dir ./dotnet-sdk

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
# Method 1: Create _redirects file - tells Cloudflare to serve index.html for all routes
echo "/* /index.html 200" > ./dist/wwwroot/_redirects

# Method 2: Copy index.html to 404.html as fallback
cp ./dist/wwwroot/index.html ./dist/wwwroot/404.html

echo "Build and SPA routing configuration complete."
echo "Output directory contents:"
ls -la ./dist/wwwroot/