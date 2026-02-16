#!/usr/bin/env bash
set -e 

# --- CONFIGURATION ---
# 1. Change this to your subfolder name (e.g., "ClientApp" or "MyBlazorApp")
PROJECT_FOLDER="AbeGamingBlazorApp" 
# ---------------------

echo "Installing .NET 10.0..."

# 1. Get the install script
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh

# 2. Install .NET 10
./dotnet-install.sh --channel 10.0 --install-dir ./dotnet-sdk

# 3. Publish the project
# This will put everything into a folder named 'dist' in the root
echo "Building $PROJECT_FOLDER..."
./dotnet-sdk/dotnet publish "$PROJECT_FOLDER" -c Release -o ./dist