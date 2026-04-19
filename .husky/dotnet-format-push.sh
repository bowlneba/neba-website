#!/bin/sh
dotnet format
if ! git diff --quiet; then
  git add -u
  git commit -m "Husky: dotnet format"
  echo "dotnet format made changes and committed them. Please push again to include the format commit."
  exit 1
fi
