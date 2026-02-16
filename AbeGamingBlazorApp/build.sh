#!/usr/bin/env bash
curl -sSL  > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 8.0 -InstallDir ./dotnet
./dotnet/dotnet publish -c Release -o output