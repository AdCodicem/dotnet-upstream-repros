#!/usr/bin/env bash
# A server error raised while EF Core reads the rows of a query reaches no IDbCommandInterceptor method: CommandFailed
# fires only when the error is raised by ExecuteReader.
#   ./repro.sh                                                              .NET 10, EF Core 10.0.13
#   REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 ./repro.sh     .NET 11 RC1
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
export REPRO_TFM="${REPRO_TFM:-net10.0}" REPRO_EF_VERSION="${REPRO_EF_VERSION:-10.0.13}"
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, EF Core $REPRO_EF_VERSION, $(uname -s)"
  rm -rf bin obj
  dotnet run 2>&1 | grep -v ': warning '
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
