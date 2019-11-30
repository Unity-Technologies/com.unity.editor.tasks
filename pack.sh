#!/bin/sh -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

OS="Mac"
if [[ -e "/c/" ]]; then
  OS="Windows"
fi

CONFIGURATION=Release
PUBLIC=""
BUILD=0
UPM=0

while (( "$#" )); do
  case "$1" in
    -d|--debug)
      CONFIGURATION="Debug"
      shift
    ;;
    -r|--release)
      CONFIGURATION="Release"
      shift
    ;;
    -p|--public)
      PUBLIC="/p:PublicRelease=true"
      shift
    ;;
    -b|--build)
      BUILD=1
      shift
    ;;
    -u|--upm)
      UPM=1
      shift
    ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
      ;;
    *)
      shift
    ;;
  esac
done

if [[ x"$OS" == x"Windows" && x"$PUBLIC" != x"" ]]; then
  PUBLIC="/$PUBLIC"
fi

pushd $DIR >/dev/null 2>&1
if [[ x"$BUILD" == x"1" ]]; then
  dotnet restore
  dotnet build --no-restore -c $CONFIGURATION $PUBLIC
fi
dotnet pack --no-build --no-restore -c $CONFIGURATION $PUBLIC

if [[ x"$UPM" == x"1" ]]; then
  powershell scripts/Pack-Npm.ps1
else
  srcdir="$DIR/build/packages"
  targetdir="$DIR/upm-ci~/packages"
  mkdir -p $targetdir
  rm -f $targetdir/*

  cat >$targetdir/packages.json <<EOL
{
EOL

  pushd $srcdir
  count=0
  for j in `ls -d *`; do
    echo $j
    pushd $j
    tgz="$(npm pack -q)"
    mv -f $tgz $targetdir/$tgz
    cp package.json $targetdir/$tgz.json
    popd

    comma=""
    if [[ x"$count" == x"1" ]]; then comma=","; fi
    json="$(cat $targetdir/$tgz.json)"
    cat >>$targetdir/packages.json <<EOL
    ${comma}
    "${tgz}": ${json}
EOL

    count=1
  done
  popd

  cat >>$targetdir/packages.json <<EOL
}
EOL
fi
popd >/dev/null 2>&1
