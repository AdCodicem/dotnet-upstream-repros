#!/usr/bin/env bash
# Compiled model: a ValueComparer<T> configured for a struct T, applied to a T? property, is written as
# new NullableValueComparer<TheComparerClass>(...), which does not compile (CS0453).
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
step() { echo; echo "== $*"; }
# dotnet-ef is installed under .tools as an apphost, which finds .NET through DOTNET_ROOT rather than PATH:
# default it to the dotnet on PATH, so that an SDK installed in a folder (dotnet-install.sh, a preview) works.
dotnet_dir="$(dirname "$(readlink -f "$(command -v dotnet)")")"
if [ -z "${DOTNET_ROOT:-}" ] && [ -d "$dotnet_dir/host/fxr" ]; then
  export DOTNET_ROOT="$(cygpath -w "$dotnet_dir" 2>/dev/null || echo "$dotnet_dir")"
fi
export REPRO_TFM="${REPRO_TFM:-net10.0}" REPRO_EF_VERSION="${REPRO_EF_VERSION:-10.0.13}"
{
  echo "== .NET SDK $(dotnet --version), $REPRO_TFM, EF Core $REPRO_EF_VERSION, $(uname -s)"
  rm -rf bin obj Compiled .tools repro.db
  dotnet tool install dotnet-ef --version "$REPRO_EF_VERSION" --tool-path .tools >/dev/null || { echo "installing dotnet-ef failed, stopping"; exit 1; }

  step "Control, model built at run time: dotnet run"
  dotnet run 2>&1 | grep -v ': warning '

  step "dotnet ef dbcontext optimize --output-dir Compiled --namespace Compiled"
  .tools/dotnet-ef dbcontext optimize --output-dir Compiled --namespace Compiled 2>&1 | grep -v ': warning ' \
    || { echo "dotnet ef failed, stopping"; exit 1; }

  step "What the compiled model writes for Backordered's comparer: grep NullableValueComparer Compiled/*.cs"
  grep -n 'NullableValueComparer' Compiled/*.cs | sed -e 's/^\(.*\.cs:[0-9]*:\) */\1 /'

  step "The compiled model does not build: dotnet build"
  dotnet build 2>&1 | grep -E ' error ' | sed -e 's/ \[[^]]*\]$//' | sort -u

  step "Workaround, a ValueComparer<Qty?> of its own for Qty?: Workaround=true dotnet ef dbcontext optimize, then dotnet run"
  rm -rf Compiled
  export Workaround=true
  .tools/dotnet-ef dbcontext optimize --output-dir Compiled --namespace Compiled 2>&1 | grep -v ': warning ' \
    || { echo "dotnet ef failed, stopping"; exit 1; }
  grep -n 'NullableQtyComparer()' Compiled/*.cs | sed -e 's/^\(.*\.cs:[0-9]*:\) */\1 /'
  dotnet run 2>&1 | grep -v ': warning '
} 2>&1 | redact | tee "repro-output-$REPRO_TFM.txt"
