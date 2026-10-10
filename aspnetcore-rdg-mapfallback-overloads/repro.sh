#!/usr/bin/env bash
# Builds the repro from scratch with the Request Delegate Generator off, then on. Off, it builds and runs. On, the
# build fails: the script prints the compiler's error, then the interceptor the generator wrote.
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
# Prints each build error once and returns the exit code of the build. GenerateFullPaths=false makes the compiler
# name the generated file relative to this folder, so that its error is printed wherever this folder is. The file
# sits 191 characters below it, and MSBuild looks for an error in the first 400 characters of a line: on Windows,
# with SDK 10.0.401, a plain dotnet build printed the CS9144 line when the full path of the file was 375 characters
# long, and only MSB6006 ("csc.exe" exited with code 1) when it was 383.
build() { dotnet build -v q -p:GenerateFullPaths=false "$@" 2>&1 | grep ': error ' | awk '!seen[$0]++'; return "${PIPESTATUS[0]}"; }
export REPRO_TFM="${REPRO_TFM:-net10.0}"
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, $(uname -s)"

  step "EnableRequestDelegateGenerator=false"
  rm -rf bin obj
  build -p:EnableRequestDelegateGenerator=false || { echo "build failed, stopping"; exit 1; }
  dotnet run --no-build -p:EnableRequestDelegateGenerator=false

  step "EnableRequestDelegateGenerator=true"
  rm -rf bin obj
  if build; then
    dotnet run --no-build
  else
    echo "build failed"
    # The data of each InterceptsLocation is elided: an LF and a CRLF checkout of Program.cs give different data.
    step "The interceptor, in GeneratedRouteBuilderExtensions.g.cs (location data elided)"
    awk '/InterceptsLocationAttribute\(1,/ { on = 1 } on { sub(/\(1, "[^"]*"\)/, "(1, \"...\")"); print } on && /Delegate handler\)/ { exit }' \
      "$(find obj -name GeneratedRouteBuilderExtensions.g.cs)"
  fi
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
