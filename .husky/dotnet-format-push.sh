#!/bin/sh
resolve_dotnet() {
  if command -v dotnet >/dev/null 2>&1; then
    command -v dotnet
    return 0
  fi

  for candidate in \
    "$HOME/.dotnet/dotnet" \
    "/opt/homebrew/bin/dotnet" \
    "/usr/local/bin/dotnet" \
    "/usr/share/dotnet/dotnet"; do
    if [ -x "$candidate" ]; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done

  return 1
}

DOTNET_BIN="$(resolve_dotnet)"
if [ -z "$DOTNET_BIN" ]; then
  echo "Husky dotnet-format-push: dotnet was not found in PATH or common install locations."
  exit 127
fi

"$DOTNET_BIN" build
if [ $? -ne 0 ]; then
  echo "dotnet build failed. Push aborted."
  exit 1
fi

"$DOTNET_BIN" format
if ! git diff --quiet; then
  git add -u
  git commit -m "Husky: dotnet format"
  echo "dotnet format made changes and committed them. Please push again to include the format commit."
  exit 1
fi
