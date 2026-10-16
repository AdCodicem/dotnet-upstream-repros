#!/usr/bin/env bash
# Microsoft.AspNetCore.JsonPatch (Newtonsoft.Json): how ApplyTo converts, copies, tests and moves values, and what it throws.
#   ./repro.sh                      .NET 10, Microsoft.AspNetCore.JsonPatch 10.0.13
#   REPRO_TFM=net11.0 ./repro.sh    .NET 11 (needs an 11.0 SDK), Microsoft.AspNetCore.JsonPatch 11.0.0-rc.1.26425.128
#   REPRO_JSONPATCH_VERSION=<version> picks another version of the package.
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
export REPRO_TFM="${REPRO_TFM:-net10.0}"
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, $(uname -s)"
  rm -rf bin obj
  dotnet build -c Release -v q 2>&1 | grep ': error ' ; [ "${PIPESTATUS[0]}" = 0 ] || { echo "build failed, stopping"; exit 1; }
  dotnet "bin/Release/$REPRO_TFM/Repro.dll"
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
