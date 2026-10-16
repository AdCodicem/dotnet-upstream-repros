#!/usr/bin/env bash
# The C# generator writes two JsonConverter attributes on a property whose schema is an enum of format: date strings.
#   ./repro.sh    .NET 10, NSwag.CodeGeneration.CSharp 14.7.1 (NJsonSchema.CodeGeneration.CSharp 11.6.1)
# Generates C# from holiday.schema.json and openapi.json into client/, then compiles client/Client.csproj.
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
{
  echo "== .NET SDK $(dotnet --version), net10.0, $(uname -s)"
  rm -rf bin obj client/bin client/obj client/*.g.cs
  build
  dotnet bin/Release/net10.0/Repro.dll njsonschema
  dotnet bin/Release/net10.0/Repro.dll nswag

  step "dotnet build client/Client.csproj (the three generated files)"
  dotnet build client/Client.csproj -c Release -v q 2>&1 | grep -E ': (error|warning) ' | sed -E 's/ \[[^]]*\]$//' | sort -u
  [ "${PIPESTATUS[0]}" = 0 ] && echo "Build succeeded." || echo "Build failed."
} 2>&1 | redact | tee repro-output-net10.0.txt
