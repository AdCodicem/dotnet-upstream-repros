#!/usr/bin/env bash
# NullReferenceException when the element value converter of a primitive collection returns null for a non-null element.
#   ./repro.sh                                                              .NET 10, EF Core 10.0.12
#   REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 ./repro.sh     .NET 11 RC1
#   REPRO_DOCKER=1 ./repro.sh                    also writes through the SQL Server provider, on a SQL Server 2022
#                                                container it starts and removes
#   REPRO_SQLSERVER='Server=...;...' ./repro.sh  the same on a server of your own (no Database= in the string)
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
export REPRO_TFM="${REPRO_TFM:-net10.0}" REPRO_EF_VERSION="${REPRO_EF_VERSION:-10.0.12}"
if [ "${REPRO_DOCKER:-0}" = 1 ] && [ -z "${REPRO_SQLSERVER:-}" ]; then
  container=repro-efcore-null-element
  password='Repro_Passw0rd!'
  docker rm -f "$container" >/dev/null 2>&1
  docker run -d --name "$container" -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=$password" -p 14337:1433 \
    mcr.microsoft.com/mssql/server:2022-latest >/dev/null || { echo "starting SQL Server failed, stopping"; exit 1; }
  trap 'docker rm -f "$container" >/dev/null 2>&1' EXIT
  for _ in $(seq 60); do
    docker exec "$container" /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$password" -Q 'SELECT 1' >/dev/null 2>&1 && break
    sleep 2
  done
  export REPRO_SQLSERVER="Server=127.0.0.1,14337;User Id=sa;Password=$password;TrustServerCertificate=true"
fi
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, EF Core $REPRO_EF_VERSION, $(uname -s)"
  rm -rf bin obj variants/bin variants/obj

  step "The repro: dotnet run"
  dotnet run 2>&1 | grep -v ': warning '

  step "Variants: dotnet run --project variants"
  dotnet run --project variants 2>&1 | grep -v ': warning '

  step "Variants, the collection parameter sent as one JSON parameter: dotnet run --project variants -- --parameter-mode"
  dotnet run --project variants --no-build -- --parameter-mode 2>&1 | grep -v ': warning '
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
