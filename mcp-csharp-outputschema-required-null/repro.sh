#!/usr/bin/env bash
# UseStructuredContent: outputSchema lists a nullable property as required, structuredContent omits it when null.
#   ./repro.sh    .NET 10, and Node.js for the TypeScript SDK's client under client/
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
  echo "== .NET SDK $(dotnet --version), net10.0, Node.js $(node --version), $(uname -s)"
  rm -rf bin obj client/node_modules
  build

  step "The SDK's own client, in process: dotnet bin/Release/net10.0/Repro.dll"
  dotnet bin/Release/net10.0/Repro.dll

  (cd client && npm ci --no-audit --no-fund >/dev/null 2>&1) || { echo "npm ci failed, stopping"; exit 1; }
  step "The TypeScript SDK's client over stdio: node client/client.mjs bin/Release/net10.0/Repro.dll"
  node client/client.mjs bin/Release/net10.0/Repro.dll

  step "Serializer options that keep nulls: node client/client.mjs bin/Release/net10.0/Repro.dll --keep-nulls"
  node client/client.mjs bin/Release/net10.0/Repro.dll --keep-nulls
} 2>&1 | redact | tee repro-output-net10.0.txt
