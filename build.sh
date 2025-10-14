#!/usr/bin/env bash
# build.sh - lightweight wrapper
# Usage: ./build.sh --target Pack --semver 1.2.3 --prerelease rc.1 --push false

set -euo pipefail

TARGET="Default"
CONFIGURATION="Release"
SEMVER=""
PRERELEASE=""
SKIP_TESTS="false"
SKIP_INTEG="false"
PUSH="false"
NUGET_SOURCE=""
SOLUTION="./Maurer.OktaFilter/Maurer.OktaFilter.sln"
PROJECT="./Maurer.OktaFilter/Maurer.OktaFilter/Maurer.OktaFilter.csproj"

while [[ $# -gt 0 ]]; do
  case $1 in
    --target) TARGET="$2"; shift ;;
    --configuration) CONFIGURATION="$2"; shift ;;
    --semver) SEMVER="$2"; shift ;;
    --prerelease) PRERELEASE="$2"; shift ;;
    --skipTests) SKIP_TESTS="$2"; shift ;;
    --skipIntegration) SKIP_INTEG="$2"; shift ;;
    --push) PUSH="$2"; shift ;;
    --nugetSource) NUGET_SOURCE="$2"; shift ;;
    --solution) SOLUTION="$2"; shift ;;
    --project) PROJECT="$2"; shift ;;
    *) echo "Unknown arg: $1" ; exit 1 ;;
  esac
  shift
done

dotnet tool restore

CMD=( dotnet cake --target "$TARGET" --configuration "$CONFIGURATION" --solution "$SOLUTION" --project "$PROJECT" )
[[ -n "$SEMVER" ]] && CMD+=( --semver "$SEMVER" )
[[ -n "$PRERELEASE" ]] && CMD+=( --prerelease "$PRERELEASE" )
[[ "$SKIP_TESTS" == "true" ]] && CMD+=( --skipTests true )
[[ "$SKIP_INTEG" == "true" ]] && CMD+=( --skipIntegration true )
[[ "$PUSH" == "true" ]] && CMD+=( --push true )
[[ -n "$NUGET_SOURCE" ]] && CMD+=( --nugetSource "$NUGET_SOURCE" )

echo "${CMD[@]}"
"${CMD[@]}"
