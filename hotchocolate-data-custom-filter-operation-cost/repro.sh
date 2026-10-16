#!/usr/bin/env bash
# HotChocolate.Data: a filter operation type of the application's own cannot be weighed as expensive by the cost
# analysis, since the flag behind `.Expensive()` is internal; the same operation costs 10 instead of 20.
#   ./repro.sh                                     .NET 10, HotChocolate 16.6.8 (the latest 16.x release)
#   REPRO_HC_VERSION=16.7.0-p.28 ./repro.sh        the latest 16.7 preview
# No database server is needed. The output is also saved to repro-output-net10.0[-hotchocolate-<version>].txt.
set -u -o pipefail
cd "$(dirname "$0")"
export DOTNET_CLI_UI_LANGUAGE=en DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
# Shortens this folder's path to <repro> and the home directory to <home> in what is printed and saved.
export here="$(pwd)" here_native="$(cygpath -w "$(pwd)" 2>/dev/null || pwd)"
export home="$HOME" home_native="$(cygpath -w "$HOME" 2>/dev/null || echo "$HOME")"
redact() {
  awk 'function sub_all(s, from, to,   i) { while (from != "" && (i = index(s, from)) > 0) s = substr(s, 1, i - 1) to substr(s, i + length(from)); return s }
       { $0 = sub_all($0, ENVIRON["here_native"], "<repro>"); $0 = sub_all($0, ENVIRON["here"], "<repro>")
         $0 = sub_all($0, ENVIRON["home_native"], "<home>"); $0 = sub_all($0, ENVIRON["home"], "<home>"); print }'
}
step() { echo; echo "== $*"; }
export REPRO_HC_VERSION="${REPRO_HC_VERSION:-16.6.8}"
out="repro-output-net10.0.txt"
[ "$REPRO_HC_VERSION" = 16.6.8 ] || out="repro-output-net10.0-hotchocolate-$REPRO_HC_VERSION.txt"
{
  echo "== .NET SDK $(dotnet --version), net10.0, HotChocolate $REPRO_HC_VERSION, $(uname -s)"
  rm -rf bin obj

  step "dotnet run"
  dotnet run 2>&1 | grep -v ': warning '
} 2>&1 | redact | tee "$out"
