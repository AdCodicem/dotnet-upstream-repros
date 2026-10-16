#!/usr/bin/env bash
# Builds the repro from scratch with a generic formatter registered, then with a closed subclass of it.
#   ./repro.sh                                          Akka.Serialization.V2 1.6.0-beta3
#   REPRO_AKKA_VERSION=1.6.0-beta2 ./repro.sh           another version
# The output is also saved to repro-output-net10.0.txt.
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
generated() { find obj -name 'ReproSerializer.AkkaSerialization.g.cs' | head -n 1; }
export REPRO_AKKA_VERSION="${REPRO_AKKA_VERSION:-1.6.0-beta3}"
{
  echo "== .NET SDK $(dotnet --version), net10.0, Akka.Serialization.V2 $REPRO_AKKA_VERSION, $(uname -s)"

  step "[AkkaSerializerFormatter<Sku, StringValueFormatter<Sku>>]: dotnet build"
  rm -rf bin obj
  dotnet build -v q -clp:NoSummary 2>&1 | grep ': error ' | sort -u
  [ "${PIPESTATUS[0]}" = 0 ] && echo "build succeeded"
  echo "-- the formatter field and its construction in $(generated):"
  grep -n -E '_akkaFormatter_[A-Za-z0-9_]+( =|;)' "$(generated)"

  step "Workaround, [AkkaSerializerFormatter<Sku, SkuFormatter>] with SkuFormatter : StringValueFormatter<Sku>: dotnet build -p:Workaround=true"
  rm -rf bin obj
  dotnet build -v q -clp:NoSummary -p:Workaround=true 2>&1 | grep ': error ' | sort -u
  [ "${PIPESTATUS[0]}" = 0 ] || { echo "build failed, stopping"; exit 1; }
  echo "build succeeded"
  echo "-- the formatter field and its construction in $(generated):"
  grep -n -E '_akkaFormatter_[A-Za-z0-9_]+( =|;)' "$(generated)"
  dotnet run --no-build -p:Workaround=true
} 2>&1 | redact | tee "repro-output-net10.0.txt"
