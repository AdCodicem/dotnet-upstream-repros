#!/usr/bin/env bash
# A dictionary key served by a CategoryScalar serializer is never a protobuf map: not in the export, not for duplicate keys.
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
# The protoc that Grpc.Tools carries, for this OS.
protoc_path() {
  local root="${NUGET_PACKAGES:-$HOME/.nuget/packages}/grpc.tools/2.84.0/tools"
  case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) echo "$root/windows_x64/protoc.exe" ;;
    Darwin) echo "$root/macosx_x64/protoc" ;;
    *) echo "$root/linux_x64/protoc" ;;
  esac
}
{
  echo "== .NET SDK $(dotnet --version), net10.0, $(uname -s)"
  rm -rf bin obj
  build
  dotnet bin/Release/net10.0/Repro.dll

  protoc="$(protoc_path)"
  chmod +x "$protoc" 2>/dev/null
  step "$("$protoc" --version) on the Catalogue export"
  "$protoc" --proto_path=obj/proto --descriptor_set_out=obj/proto/catalogue.pb catalogue.proto && echo "accepted"
} 2>&1 | redact | tee repro-output-net10.0.txt
