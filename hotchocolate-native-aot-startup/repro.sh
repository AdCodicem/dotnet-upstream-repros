#!/usr/bin/env bash
# HotChocolate: the smallest application, its schema written for the source generator, answers under the JIT and aborts
# at start as a native AOT binary ("A suitable constructor for type
# 'HotChocolate.Fetching.DataLoaderRootFieldTypeInterceptor' could not be located."), unless HotChocolate.Types is
# rooted (TrimmerRootAssembly).
#   ./repro.sh                                     .NET 10, HotChocolate 16.6.8 (the latest 16.x release)
#   REPRO_HC_VERSION=16.7.0-p.28 ./repro.sh        the latest 16.7 preview
# The output is also saved to repro-output-net10.0[-hotchocolate-<version>].txt.
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
# Native publishing needs the platform's native toolchain (on Windows, the Visual Studio C++ build tools; the
# ILCompiler targets look for vswhere.exe, which this script puts on PATH when it is installed but not found).
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) rid=win-x64; exe=.exe
    command -v vswhere.exe >/dev/null 2>&1 || export PATH="$PATH:/c/Program Files (x86)/Microsoft Visual Studio/Installer" ;;
  Darwin) rid="osx-$(uname -m | sed 's/x86_64/x64/')"; exe= ;;
  *) rid="linux-$(uname -m | sed 's/x86_64/x64/; s/aarch64/arm64/')"; exe= ;;
esac
{
  echo "== .NET SDK $(dotnet --version), net10.0, $rid, HotChocolate $REPRO_HC_VERSION, $(uname -s)"
  rm -rf bin obj publish; mkdir -p publish

  step "Under the JIT: dotnet run -c Release"
  dotnet run -c Release 2>&1 | grep -v ': warning '

  step "Native AOT: dotnet publish -c Release -r $rid (trimming and AOT warnings, counted by code and assembly)"
  dotnet publish -c Release -r "$rid" -o publish/plain > publish/plain.log 2>&1 || { grep -E ': error ' publish/plain.log; echo "publish failed, stopping"; exit 1; }
  grep -oE 'warning IL[0-9]+: [A-Za-z]+(\.[A-Za-z]+)*' publish/plain.log | sed -E 's/: ([A-Za-z]+)\.([A-Za-z]+).*/ \1.\2/' | sort | uniq -c
  "./publish/plain/Repro$exe"; echo "exit code $?"

  step "Native AOT, HotChocolate.Types rooted: dotnet publish -c Release -r $rid -p:ReproRoot=true"
  rm -rf obj
  dotnet publish -c Release -r "$rid" -o publish/rooted -p:ReproRoot=true > publish/rooted.log 2>&1 || { grep -E ': error ' publish/rooted.log; echo "publish failed, stopping"; exit 1; }
  "./publish/rooted/Repro$exe"; echo "exit code $?"
} 2>&1 | redact | tee "$out"
