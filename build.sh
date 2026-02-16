#!/usr/bin/env bash
set -e 

# --- CONFIGURATION ---
PROJECT_FOLDER="AbeGamingBlazorApp" 
# ---------------------

echo "Installing .NET 10.0..."
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0 --install-dir ./dotnet-sdk

echo "Building $PROJECT_FOLDER..."
./dotnet-sdk/dotnet publish "$PROJECT_FOLDER" -c Release -o ./dist

# --- SPA ROUTING FIX ---
# This tells Cloudflare to send all traffic to index.html for internal routing
echo "/* /index.html 200" > ./dist/wwwroot/_redirects

# This prevents Cloudflare from ignoring folders starting with underscores
touch ./dist/wwwroot/.nojekyll

echo "Build and Routing configuration complete."