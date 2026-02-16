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
git log --pretty=format:'{%n  "hash": "%H",%n  "shortHash": "%h",%n  "date": "%ci",%n  "author": "%an",%n  "message": "%s"%n},' HEAD | sed '$ s/,$//' | sed '1s/^/[/' | sed '$s/$/]/' > "$PROJECT_FOLDER/wwwroot/changelog.json"
echo "Changelog generated with $(git rev-list --count HEAD) commits."

echo "Building $PROJECT_FOLDER..."
./dotnet-sdk/dotnet publish "$PROJECT_FOLDER" -c Release -o ./dist

# --- SPA ROUTING FIX ---
# Copy index.html to 404.html - Cloudflare serves 404.html for unknown routes,
# allowing Blazor's client-side router to handle the navigation
cp ./dist/wwwroot/index.html ./dist/wwwroot/404.html

echo "Build and SPA routing configuration complete."