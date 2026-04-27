#!/usr/bin/env bash
set -euo pipefail
export Jwt__Key="local-dev-jwt-key-must-be-at-least-32-bytes-long-for-hs256!"
exec dotnet run --project "$(dirname "$0")/Backend/PokemonLocations.TokenIssuer" -- "$@"
