#!/bin/sh
# Compile and run the standalone level reachability audit (no editor).
# Uses Unity's bundled Mono Roslyn csc + UnityEngine.CoreModule for the
# math types; the level library itself is pure C# data.
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data"
MB="$UNITY/MonoBleedingEdge"
MONO="$MB/bin/mono.exe"
CSC_EXE="$MB/lib/mono/4.5/csc.exe"
CORE="$UNITY/Managed/UnityEngine/UnityEngine.CoreModule.dll"

OUT="$ROOT/tools/LevelAudit/audit.exe"
cd "$ROOT"
# UnityEngine.CoreModule's Vector3 derives from ValueType over in
# netstandard — use MONO's own facade (Unity's ref/netstandard.dll
# conflicts with mono's mscorlib under Roslyn: CS0518 everywhere).
"$MONO" "$CSC_EXE" -nologo -target:exe -out:"$OUT" \
  -r:"$MB/lib/mono/4.5/mscorlib.dll" \
  -r:"$MB/lib/mono/4.5/Facades/netstandard.dll" \
  -r:"$CORE" \
  Assets/Scripts/LevelDefinition.cs \
  Assets/Scripts/LevelLibrary.cs \
  Assets/Scripts/Remixes.cs \
  Assets/Scripts/LevelPackTwo.cs \
  Assets/Scripts/LevelPackThree.cs \
  Assets/Scripts/LevelPackFour.cs \
  Assets/Scripts/LevelPackFive.cs \
  Assets/Scripts/LevelPackSix.cs \
  Assets/Scripts/LevelPackSeven.cs \
  Assets/Scripts/LevelPackEight.cs \
  Assets/Scripts/LevelPackNine.cs \
  Assets/Scripts/LevelPackTen.cs \
  Assets/Scripts/LevelPackBSides.cs \
  Assets/Scripts/LevelPackEleven.cs \
  Assets/Scripts/LevelPackTwelve.cs \
  Assets/Scripts/LevelPackThirteen.cs \
  Assets/Scripts/LevelReachability.cs \
  Assets/Scripts/HazardTiming.cs \
  tools/LevelAudit/Program.cs

# CoreModule must be on mono's assembly load path at runtime.
MONO_PATH="$UNITY/Managed/UnityEngine" "$MONO" "$OUT" "$@"
