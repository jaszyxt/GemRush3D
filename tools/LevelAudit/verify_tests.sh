#!/bin/sh
# Syntax-gate: compile the level data + engine + the EditMode audit tests
# against Unity's CoreModule, using Unity's bundled mono Roslyn.
# Mirrors tools/LevelAudit/run.sh but includes the test file, so a change
# to the tests is verified before it reaches the editor.
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data"
MB="$UNITY/MonoBleedingEdge"
MONO="$MB/bin/mono.exe"
CSC_EXE="$MB/lib/mono/4.5/csc.exe"
CORE="$UNITY/Managed/UnityEngine/UnityEngine.CoreModule.dll"
NUNIT="$ROOT/Library/PackageCache/com.unity.ext.nunit@0198eae3b53e/net472/unity-custom/nunit.framework.dll"

cd "$ROOT"
"$MONO" "$CSC_EXE" -nologo -target:library \
  -out:"$ROOT/tools/LevelAudit/verify.dll" \
  -r:"$MB/lib/mono/4.5/mscorlib.dll" \
  -r:"$MB/lib/mono/4.5/Facades/netstandard.dll" \
  -r:"$CORE" -r:"$NUNIT" \
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
  Assets/Scripts/GoldenGem.cs \
  Assets/Scripts/SaveSystem.cs \
  Assets/Tests/EditMode/LevelAuditTests.cs 2>&1 \
  | grep -v "CS0246\|CS0103\|CS0012" || true
rm -f "$ROOT/tools/LevelAudit/verify.dll"
echo "verify gate done (no output above = clean)"
