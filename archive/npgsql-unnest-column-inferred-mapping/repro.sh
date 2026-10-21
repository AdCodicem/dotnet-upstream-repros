#!/usr/bin/env bash
# Npgsql: Union/Except over two array columns throws "Conflicting type mappings were inferred for column 'value'",
# because NpgsqlTypeMappingPostprocessor infers the type mapping of an unnest over a column.
#   ./repro.sh                 translation only (ToQueryString), no server: .NET 10, Npgsql 10.0.3, EF Core 10.0.12
#   REPRO_DOCKER=1 ./repro.sh  also runs the queries, as shipped and with the proposed change, on a PostgreSQL 17
#                              container it starts and removes
#   REPRO_POSTGRES='Host=...;Database=...' ./repro.sh   the same on a server of your own (drops and creates the database)
#   REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 REPRO_NPGSQL_VERSION=11.0.0-rc.1.1 ./repro.sh   .NET 11 RC1
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
export REPRO_TFM="${REPRO_TFM:-net10.0}" REPRO_EF_VERSION="${REPRO_EF_VERSION:-10.0.12}" REPRO_NPGSQL_VERSION="${REPRO_NPGSQL_VERSION:-10.0.3}"
server="no server"
if [ "${REPRO_DOCKER:-0}" = 1 ] && [ -z "${REPRO_POSTGRES:-}" ]; then
  container=repro-npgsql-unnest
  docker rm -f "$container" >/dev/null 2>&1
  docker run -d --name "$container" -e POSTGRES_PASSWORD=repro -p 54329:5432 postgres:17-alpine >/dev/null \
    || { echo "starting PostgreSQL failed, stopping"; exit 1; }
  trap 'docker rm -f "$container" >/dev/null 2>&1' EXIT
  for _ in $(seq 60); do
    docker exec "$container" psql -U postgres -h 127.0.0.1 -tAc 'SELECT 1' >/dev/null 2>&1 && break
    sleep 1
  done
  server="PostgreSQL $(docker exec "$container" psql -U postgres -h 127.0.0.1 -tAc 'SHOW server_version')"
  export REPRO_POSTGRES="Host=127.0.0.1;Port=54329;Username=postgres;Password=repro;Database=repro_unnest"
elif [ -n "${REPRO_POSTGRES:-}" ]; then
  server="a PostgreSQL server"
fi
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, Npgsql.EntityFrameworkCore.PostgreSQL $REPRO_NPGSQL_VERSION, EF Core $REPRO_EF_VERSION, $server, $(uname -s)"
  rm -rf bin obj fix-check/bin fix-check/obj

  step "The repro: dotnet run"
  dotnet run 2>&1 | grep -v ': warning '

  step "The proposed change: dotnet run --project fix-check"
  dotnet run --project fix-check 2>&1 | grep -v ': warning '
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
