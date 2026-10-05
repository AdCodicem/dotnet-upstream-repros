#!/usr/bin/env bash
# Precompiled query throws InvalidCastException materializing a property whose value converter is a ValueConverter subclass.
#   ./repro.sh                                                              .NET 10, EF Core 10.0.12
#   REPRO_TFM=net11.0 REPRO_EF_VERSION=11.0.0-rc.1.26425.128 ./repro.sh     .NET 11 RC1
#   REPRO_PUBLISH_AOT=1 ./repro.sh                                          also publishes and runs the native AOT binary
#     (needs https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites)
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
# dotnet-ef is installed under .tools as an apphost, which finds .NET through DOTNET_ROOT rather than PATH:
# default it to the dotnet on PATH, so that an SDK installed in a folder (dotnet-install.sh, a preview) works.
dotnet_dir="$(dirname "$(readlink -f "$(command -v dotnet)")")"
if [ -z "${DOTNET_ROOT:-}" ] && [ -d "$dotnet_dir/host/fxr" ]; then
  export DOTNET_ROOT="$(cygpath -w "$dotnet_dir" 2>/dev/null || echo "$dotnet_dir")"
fi
export REPRO_TFM="${REPRO_TFM:-net10.0}" REPRO_EF_VERSION="${REPRO_EF_VERSION:-10.0.12}"
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, EF Core $REPRO_EF_VERSION, $(uname -s)"
  rm -rf bin obj Compiled publish .tools repro.db

  step "Control, model built at run time: dotnet run -p:PublishAot=false"
  dotnet run -p:PublishAot=false 2>&1 | grep -v ': warning '

  step "dotnet ef dbcontext optimize --output-dir Compiled --namespace Compiled --precompile-queries --nativeaot"
  dotnet tool install dotnet-ef --version "$REPRO_EF_VERSION" --tool-path .tools >/dev/null || { echo "installing dotnet-ef failed, stopping"; exit 1; }
  .tools/dotnet-ef dbcontext optimize --output-dir Compiled --namespace Compiled --precompile-queries --nativeaot 2>&1 | grep -v ': warning ' \
    || { echo "dotnet ef failed, stopping"; exit 1; }

  step "Precompiled queries under the JIT: dotnet run"
  dotnet run 2>&1 | grep -v ': warning '

  if [ "${REPRO_PUBLISH_AOT:-0}" = 1 ]; then
    step "Native AOT: dotnet publish -o publish, then the binary"
    dotnet publish -o publish 2>&1 | grep -E ' error |->' | grep -v ': warning ' || { echo "publish failed, stopping"; exit 1; }
    if [ -x publish/Repro.exe ]; then ./publish/Repro.exe; else ./publish/Repro; fi
  fi
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
