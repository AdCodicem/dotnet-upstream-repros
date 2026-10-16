#!/usr/bin/env bash
# Builds the repro from scratch, prints what the regex generator emitted for each declaration, then runs it
# with CurrentCulture en-US and tr-TR.
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
  rm -rf bin obj
  dotnet build -v q 2>&1 | grep ': error ' ; [ "${PIPESTATUS[0]}" = 0 ] || { echo "build failed, stopping"; exit 1; }
  generated="$(find obj -name RegexGenerator.g.cs | head -n 1)"

  step "What the generator emitted ($generated)"
  grep -o 'GeneratedCodeAttribute("System.Text.RegularExpressions.Generator", "[^"]*")' "$generated" | head -n 1
  # Each property, the implementation it returns, and the first set its own doc comment says it matches.
  awk '/Match a character in the set \[/ && set == "" { match($0, /set \[[^]]*\]/); set = substr($0, RSTART + 4, RLENGTH - 4) }
       /public static partial global::System.Text.RegularExpressions.Regex/ {
         name = $0; sub(/.*Regex /, "", name); sub(/ =>.*/, "", name)
         impl = $0; sub(/.*Generated\./, "", impl); sub(/\.Instance;.*/, "", impl)
         printf "   %-30s -> %-31s doc comment: set %s\n", name, impl, set; set = "" }' "$generated"
  grep -E 'internal static readonly Regex Instance = new\(' "$generated" | sed -E 's/^ +/   /'

  step "dotnet run -- en-US"
  dotnet run --no-build -- en-US

  step "dotnet run -- tr-TR"
  dotnet run --no-build -- tr-TR
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
