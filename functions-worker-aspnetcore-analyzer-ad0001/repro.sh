#!/usr/bin/env bash
# Builds each file of Cases/ from scratch and prints what each build reports. Nothing is run: the defect is an analyzer
# of the ASP.NET Core integration's package that throws during the build.
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
# A clean build: each warning and error once (the summary that repeats them is left out), then the exit code.
build() {
  rm -rf bin obj
  dotnet build -v q -p:UseSharedCompilation=false "$@" 2>&1 | awk '/^Build (succeeded|FAILED)/ { summary = 1 } !summary && /: (warning|error) /'
  echo "dotnet build: exit code ${PIPESTATUS[0]}"
}
# The console has the first line of an AD0001 message only. The compiler's error log (SARIF) has all of it, as one
# JSON string: this prints each one as text.
messages() {
  awk 'function unescape(s, from, to,   i, out) { out = ""; while ((i = index(s, from)) > 0) { out = out substr(s, 1, i - 1) to; s = substr(s, i + length(from)) } return out s }
       BEGIN { b = sprintf("%c", 92); q = sprintf("%c", 34) }
       /^ *"message": "Analyzer / {
         sub(/^ *"message": "/, ""); sub(/",?\r?$/, "")
         $0 = unescape($0, b "u000d" b "u000a", "\n"); $0 = unescape($0, b "u000a", "\n")
         $0 = unescape($0, b q, q); $0 = unescape($0, b b, b)
         print; print "" }' "$1"
}
export REPRO_TFM="${REPRO_TFM:-net10.0}"
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, $(uname -s)"

  step "Cases/if-else.cs: ConfigureFunctionsWebApplication in one branch, ConfigureFunctionsWorkerDefaults in the other"
  build -p:Case=if-else -p:ErrorLog=obj/build.sarif

  step "the AD0001 message in full, from the compiler's error log"
  messages obj/build.sarif

  step "Cases/if-else.cs, -p:TreatWarningsAsErrors=true"
  build -p:Case=if-else -p:TreatWarningsAsErrors=true

  step "Cases/statement.cs: hostBuilder.ConfigureFunctionsWorkerDefaults(); as a statement"
  build -p:Case=statement

  step "Cases/assigned.cs: var configured = hostBuilder.ConfigureFunctionsWorkerDefaults(); (control)"
  build -p:Case=assigned

  step "Cases/column0.cs: the statement of Cases/statement.cs at column 0 (control)"
  build -p:Case=column0
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
