#!/usr/bin/env bash
# A [ProtoModel] reaches a CategoryScalar serializer through SerializerCache.Get<TSerializer, T>(), whose
# [DynamicallyAccessedMembers] on T keeps every member of T: the trim and AOT analyzers then warn at each call
# for the members of T that protobuf-net never uses.
#   ./repro.sh                    the Roslyn analyzers (dotnet build), then a native publish and run
#   REPRO_NATIVE=0 ./repro.sh     the Roslyn analyzers only (no native toolchain needed)
# On Windows, run it from a short path: past MAX_PATH, the native link fails with LNK1104 on Repro.sourcelink.
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
# ILC looks MSVC up through vswhere, which Git Bash does not have on PATH.
installer="/c/Program Files (x86)/Microsoft Visual Studio/Installer"
[ -d "$installer" ] && export PATH="$PATH:$installer"
# Counts the warnings by id, and prints one of each with the generated line it points at.
summarize() {
  local log="$1" generated="$2"
  # MSBuild repeats each warning in its summary: count the distinct ones
  echo "$log" | grep -E 'warning IL[0-9]{4}' | sed 's/ \[.*\]$//' | sort -u | grep -oE 'warning IL[0-9]{4}' | sort | uniq -c | sed 's/^ */  /'
  echo "$log" | grep -E 'warning IL(2026|2111|3050)' | sed 's/ \[.*\]$//; s/.*\(AppModel.ProtoModel.g.cs\)/\1/' | sort -u -t: -k2,2 | head -3 | sed 's/^/  e.g. /'
  echo "$log" | grep -oE 'AppModel.ProtoModel.g.cs\([0-9]+' | grep -oE '[0-9]+$' | sort -un | while read -r line; do
    echo "  generated line $line: $(sed -n "${line}p" "$generated" | sed 's/^ *//')"
  done
}
gen=generated/protobuf-net.BuildTools/ProtoBuf.BuildTools.Generators.ProtoModelGenerator/AppModel.ProtoModel.g.cs
{
  echo "== .NET SDK $(dotnet --version), net10.0, $(uname -s)"

  step "dotnet build, PublishAot=true (Roslyn trim and AOT analyzers), Quantity without other members"
  rm -rf bin obj
  log="$(dotnet build -c Release -v q 2>&1)" || { echo "$log" | grep ': error '; echo "build failed, stopping"; exit 1; }
  summarize "$log" "obj/Release/net10.0/$gen"
  echo "  IL warnings: $(echo "$log" | grep -cE "warning IL[0-9]{4}")"

  step "dotnet build, PublishAot=true, Quantity with a [RequiresUnreferencedCode] helper and a nested TypeConverter"
  rm -rf bin obj
  log="$(dotnet build -c Release -v q -p:ReproMembers=MEMBERS 2>&1)" || { echo "$log" | grep ': error '; echo "build failed, stopping"; exit 1; }
  summarize "$log" "obj/Release/net10.0/$gen"

  if [ "${REPRO_NATIVE:-1}" != 0 ]; then
    rid="$(dotnet --info | sed -n 's/^ *RID: *//p' | head -1)"
    step "dotnet publish -r $rid (ILC), Quantity without other members"
    rm -rf bin obj publish
    log="$(dotnet publish -c Release -r "$rid" -o publish 2>&1)" || { echo "$log" | grep ': error ' | sed 's/ \[.*\]$//'; echo "publish failed, stopping"; exit 1; }
    summarize "$log" "obj/Release/net10.0/$rid/$gen"

    step "dotnet publish -r $rid (ILC), Quantity with the two members"
    rm -rf bin obj publish
    log="$(dotnet publish -c Release -r "$rid" -o publish -p:ReproMembers=MEMBERS 2>&1)" || { echo "$log" | grep ': error ' | sed 's/ \[.*\]$//'; echo "publish failed, stopping"; exit 1; }
    summarize "$log" "obj/Release/net10.0/$rid/$gen"
    step "the native binary"
    ./publish/Repro
    step "the same build's IL, on the JIT"
    dotnet bin/Release/net10.0/"$rid"/Repro.dll
  fi
} 2>&1 | redact | tee repro-output-net10.0.txt
