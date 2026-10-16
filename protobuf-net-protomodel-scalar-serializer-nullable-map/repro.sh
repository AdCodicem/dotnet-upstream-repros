#!/usr/bin/env bash
# [ProtoModel]'s generated code does not compile for a nullable member, or a dictionary, of a type whose
# [ProtoContract(Serializer = ...)] is a CategoryScalar serializer (CS1503 in the generated file).
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
generated=obj/Release/net10.0/generated/protobuf-net.BuildTools/ProtoBuf.BuildTools.Generators.ProtoModelGenerator/AppModel.ProtoModel.g.cs
# Builds with the members named; on failure, prints the errors and each generated line they point at.
variant() {
  rm -rf bin obj
  local log
  log="$(dotnet build -c Release -v q -p:ReproMembers="$1" 2>&1)"
  if [ $? = 0 ]; then
    dotnet bin/Release/net10.0/Repro.dll
  else
    echo "$log" | grep ': error ' | sed 's/ \[.*\]$//' | sort -u
    echo "$log" | grep -o 'AppModel.ProtoModel.g.cs([0-9]*,' | grep -o '[0-9]*' | sort -un | while read -r line; do
      echo "  generated line $line: $(sed -n "${line}p" "$generated" | sed 's/^ *//')"
    done
  fi
  local pbn
  pbn="$(echo "$log" | grep -o 'warning PBN[0-9]*[^[]*' | sort -u)"
  echo "  PBN diagnostics: ${pbn:-none}"
}
{
  echo "== .NET SDK $(dotnet --version), net10.0, $(uname -s)"
  step "Quantity and List<Quantity> only: both models agree"
  variant ""
  step "plus a Quantity? member"
  variant NULLABLE
  echo "  the write the generator emits for it (it compiles):"
  grep -A2 'if (tmp3.HasValue)' "$generated" | sed 's/^ */    /'
  step "plus a Dictionary<string, Quantity> member"
  variant MAP_VALUE
  step "plus a Dictionary<Quantity, string> member"
  variant MAP_KEY
} 2>&1 | redact | tee repro-output-net10.0.txt
