#!/usr/bin/env bash
# An MVC [FromQuery] model whose class hides a base property with `new` of another type: the document fails
# with AmbiguousMatchException, while MVC binds the model.
#   ./repro.sh                                       .NET 10, Swashbuckle.AspNetCore 10.3.0 then 6.9.0
#   REPRO_SWASHBUCKLE_VERSIONS="10.3.0" ./repro.sh   the versions to run, in order
#   REPRO_TFM=net11.0 ./repro.sh                     .NET 11 (needs an 11.0 SDK)
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
build() { dotnet build -c Release -v q "$@" 2>&1 | grep ': error ' ; [ "${PIPESTATUS[0]}" = 0 ] || { echo "build failed, stopping"; exit 1; }; }
export REPRO_TFM="${REPRO_TFM:-net10.0}"
versions="${REPRO_SWASHBUCKLE_VERSIONS:-10.3.0 6.9.0}"
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, $(uname -s)"
  for version in $versions; do
    step "Swashbuckle.AspNetCore $version"
    rm -rf bin obj
    build -p:REPRO_SWASHBUCKLE_VERSION="$version"
    timeout 120 dotnet "bin/Release/$REPRO_TFM/Repro.dll"
  done
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
