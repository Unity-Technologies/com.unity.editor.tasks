#!/bin/bash -eu
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
UNITYVERSION=2019.2
YAMATO=0
GITHUB=0

while (( "$#" )); do
  case "$1" in
    -d|--debug)
      CONFIGURATION="Debug"
    ;;
    -r|--release)
      CONFIGURATION="Release"
    ;;
    -p|--public)
      PUBLIC="-p:PublicRelease=true"
    ;;
    -b|--build)
      BUILD=1
    ;;
    -u|--upm)
      UPM=1
    ;;
    -c)
      shift
      CONFIGURATION=$1
    ;;
    -g|--github)
      GITHUB=1
    ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
    ;;
  esac
  shift
done

if [[ x"${YAMATO_JOB_ID:-}" != x"" ]]; then
  YAMATO=1
  export GITLAB_CI=1
  export CI_COMMIT_TAG="${GIT_TAG:-}"
  export CI_COMMIT_REF_NAME="${GIT_BRANCH:-}"
fi

if [[ x"${GITHUB_ACTIONS:-}" == x"true" ]]; then
  GITHUB=1
fi

if [[ x"$GITHUB" == x"1" ]]; then

  if [[ x"${GITHUB_TOKEN:-}" == x"" ]]; then
    echo "Can't publish to GitHub without a GITHUB_TOKEN environment variable" >&2
    popd >/dev/null 2>&1
    exit 1
  fi

  nuget sources Add -Name "GPR" -Source "https://nuget.pkg.github.com/unity-technologies/index.json" -UserName "unity-technologies" -Password ${GITHUB_TOKEN:-} -NonInteractive >/dev/null 2>&1 || true
  for p in "$DIR/build/nuget/**/*.nupkg"; do
    echo "nuget push $p -Source \"GPR\""
    #nuget push $p -Source "GPR"
  done

fi
