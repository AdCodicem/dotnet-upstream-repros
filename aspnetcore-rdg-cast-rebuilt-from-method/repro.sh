#!/usr/bin/env bash
# Runs the repro from scratch with the Request Delegate Generator off, then on.
#   ./repro.sh                      .NET 10
#   REPRO_TFM=net11.0 ./repro.sh    .NET 11 (needs an 11.0 SDK)
# The output is also saved to repro-output-<tfm>.txt.
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
export REPRO_TFM="${REPRO_TFM:-net10.0}"
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, $(uname -s)"

  step "EnableRequestDelegateGenerator=false"
  rm -rf bin obj
  dotnet build -v q -p:EnableRequestDelegateGenerator=false 2>&1 | grep ': error ' ; [ "${PIPESTATUS[0]}" = 0 ] || { echo "build failed, stopping"; exit 1; }
  dotnet run --no-build -p:EnableRequestDelegateGenerator=false

  step "EnableRequestDelegateGenerator=true"
  rm -rf bin obj
  dotnet build -v q 2>&1 | grep ': error ' ; [ "${PIPESTATUS[0]}" = 0 ] || { echo "build failed, stopping"; exit 1; }
  dotnet run --no-build
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
