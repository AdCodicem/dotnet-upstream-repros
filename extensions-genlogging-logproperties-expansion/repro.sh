#!/usr/bin/env bash
# Microsoft.Gen.Logging: a classified property that [LogProperties] expands loses its classification, Transitive logs a
# [LogPropertyIgnore] property, and Transitive on a property's own [LogProperties] is ignored.
#   ./repro.sh    .NET 10
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
build() { dotnet build -c Release -v q "$@" 2>&1 | grep ': error ' ; [ "${PIPESTATUS[0]}" = 0 ] || { echo "build failed, stopping"; exit 1; }; }
# Builds a project expected to fail, and prints its diagnostics once each, without the project path MSBuild appends.
diagnostics() { dotnet build -c Release -v q --disable-build-servers "$@" 2>&1 | grep -E ': (warning|error) ' | sed -E 's/ \[[^]]*\]$//' | sort -u; }
# The tags the generated code writes, and to which array: TagArray is logged as is, ClassifiedTagArray goes to a redactor.
tags() { grep -hoE 'partial void [A-Za-z]+|(Classified)?TagArray\[[0-9]+\] = new\("[^{][^"]*", .*' "$1"/obj/Release/net10.0/generated/Microsoft.Gen.Logging/*/Logging.g.cs | sed -E 's/^partial void (.*)/   \1:/; s/^([CT])/     \1/'; }
{
  echo "== .NET SDK $(dotnet --version), net10.0, Microsoft.Extensions.Telemetry.Abstractions 10.11.0, $(uname -s)"
  rm -rf bin obj
  build
  step "where the generated code puts each tag"
  tags .
  step "what reaches the logger provider, a redactor configured for every classification"
  dotnet bin/Release/net10.0/Repro.dll
} 2>&1 | redact | tee repro-output-net10.0.txt
