#!/usr/bin/env bash
# Microsoft.Gen.Logging: [LogProperties(Transitive = true)] overflows the compiler's stack on a cycle through an inherited property.
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
# Prints the start of the crash, the generator's recursive frame with how many times it repeats (thousands of identical
# lines), and the frames that called it.
crash() {
  dotnet build -c Release -v q --disable-build-servers 2>&1 | sed -E 's/ \[[^]]*\]$//; s/^.*Core\.targets\([0-9,]+\): error : ?//' > build.log
  grep -m1 'Stack overflow' build.log || echo "(no stack overflow)"
  local frame="$(grep -m1 GetTypePropertiesToLog build.log | sed -E 's/\(.*$/(...)/')"
  [ -n "$frame" ] && echo "$frame    <- $(grep -c 'GetTypePropertiesToLog' build.log) times"
  grep -m3 -E 'at Microsoft\.Gen\.Logging\.(Parsing\.Parser\.(ProcessLogPropertiesForParameter|GetLogTypes)|LoggingGenerator\.HandleAnnotatedTypes)\(' build.log | sed -E 's/\(.*$/(...)/'
  rm -f build.log
}
{
  echo "== .NET SDK $(dotnet --version), net10.0, Microsoft.Extensions.Telemetry.Abstractions 10.11.0, $(uname -s)"
  rm -rf */bin */obj
  step "declared: a cycle through a property Leaf declares itself (LOGGEN021, as expected)"
  (cd declared && diagnostics)
  step "inherited: the same cycle through a property Leaf inherits from Node (the compiler process crashes)"
  (cd inherited && crash)
  step "system-type: a property of type System.Type (the compiler process crashes)"
  (cd system-type && crash)
} 2>&1 | redact | tee repro-output-net10.0.txt
