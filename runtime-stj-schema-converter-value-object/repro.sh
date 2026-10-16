#!/usr/bin/env bash
# Builds and runs the repro from scratch: the JSON of a type holding a converter-backed struct, the schema
# JsonSchemaExporter exports for it, and each node TransformSchemaNode is called for.
#   ./repro.sh                      .NET 10
#   REPRO_TFM=net11.0 ./repro.sh    .NET 11 (needs an 11.0 SDK)
# The output is also saved to repro-output-<tfm>.txt.
set -u -o pipefail
cd "$(dirname "$0")"
export DOTNET_CLI_UI_LANGUAGE=en DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
export REPRO_TFM="${REPRO_TFM:-net10.0}"
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, $(uname -s)"
  echo
  rm -rf bin obj
  dotnet build -v q 2>&1 | grep ': error ' ; [ "${PIPESTATUS[0]}" = 0 ] || { echo "build failed, stopping"; exit 1; }
  dotnet run --no-build
} 2>&1 | tee "repro-output-$REPRO_TFM.txt"
