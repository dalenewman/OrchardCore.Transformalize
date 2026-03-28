#!/usr/bin/env bash
# Downloads all Bootswatch 5 theme CSS files into wwwroot/styles/<theme>/.
# Run this from the OrchardCore.BootswatchTheme project directory whenever
# you want to refresh the local fallback copies (e.g. after a Bootswatch update).
#
# Usage:
#   chmod +x download-bootswatch.sh
#   ./download-bootswatch.sh
#
# The version here should match what is referenced in ResourceManifest.cs.
# After changing the version, commit the updated wwwroot files so local
# fallback stays in sync with the CDN URL.

set -euo pipefail

VERSION="5"   # resolves to latest bootswatch 5.x on npm/jsdelivr
BASE_URL="https://cdn.jsdelivr.net/npm/bootswatch@${VERSION}/dist"
WWWROOT="$(cd "$(dirname "$0")" && pwd)/wwwroot/styles"

THEMES=(
  cerulean cosmo cyborg darkly flatly journal litera lumen lux materia
  minty morph pulse quartz sandstone simplex sketchy slate solar spacelab
  superhero united vapor yeti zephyr
)

echo "Downloading Bootswatch ${VERSION} themes to ${WWWROOT}..."

for theme in "${THEMES[@]}"; do
  dir="${WWWROOT}/${theme}"
  mkdir -p "$dir"

  curl -fsSL "${BASE_URL}/${theme}/bootstrap.min.css" -o "${dir}/bootstrap.min.css"
  curl -fsSL "${BASE_URL}/${theme}/bootstrap.css"     -o "${dir}/bootstrap.css"

  echo "  ✓ ${theme}"
done

echo "Done."
