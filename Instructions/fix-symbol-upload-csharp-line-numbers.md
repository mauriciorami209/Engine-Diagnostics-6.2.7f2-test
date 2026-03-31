# Fix Plan: C# File Names & Line Numbers Missing in Unity Cloud Diagnostics Stack Traces

## Problem Statement

After uploading debug symbols via GitHub Actions, crash stack traces in Unity Cloud Diagnostics
show native symbols only — no C# file names or line numbers. The pipeline appears to succeed
but produces unusable symbolication results.

## Root Cause Chain

Three bugs compound each other. Each one alone would break the pipeline; together they make
symbolication completely impossible.

| # | Severity | Location | Description |
|---|----------|----------|-------------|
| 1 | CRITICAL | `symbol-upload.yml:42` | `-createSymbols` flag placed in wrong step — silently ignored |
| 2 | CRITICAL | `usymtool.sh:58` | `USYMTOOL_PATH_OVERRIDE` hardcoded to wrong binary, never replaced by `sed` |
| 3 | CRITICAL | `usymtool.sh:61` + `symbol-upload.yml:60` | `IL2CPP_FILE_ROOT_OVERRIDE="."` points to repo root instead of actual IL2CPP output |
| 4 | MEDIUM   | `Assets/Settings/Build Profiles/` | `Android_CI.asset` build profile referenced in workflow does not exist |
| 5 | MINOR    | `usymtool.sh:157` | `LZMA_PATH` hardcoded to local developer machine path |

---

## Step-by-Step Fix

### Step 1 — Move `-createSymbols` to the correct step (Bug 1)

**File:** `.github/workflows/symbol-upload.yml`

The `customParameters: -createSymbols` line is currently inside `actions/upload-artifact@v4`,
which has no such field and silently ignores it. Without this flag, Unity does not generate
the `.symbols.zip` file during the build, so there is nothing meaningful to upload.

**Change:** Remove the misplaced line from the `Upload APK Artifact` step and add
`customParameters: -createSymbols` to the `Build Android Project` step's `with` block.

```yaml
# BEFORE (broken)
- name: Upload APK Artifact
  uses: actions/upload-artifact@v4
  with:
    name: MyGame-APK
    path: ${{ github.workspace }}/**/*.apk
    customParameters: -createSymbols   # ← ignored here

# AFTER (correct)
- name: Build Android Project
  uses: game-ci/unity-builder@v4
  env:
    UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
    UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
    UNITY_SERIAL: ${{ secrets.UNITY_SERIAL }}
  with:
    targetPlatform: Android
    buildsPath: ${{ github.workspace }}/Build
    customParameters: -createSymbols   # ← correct location

- name: Upload APK Artifact
  uses: actions/upload-artifact@v4
  with:
    name: MyGame-APK
    path: ${{ github.workspace }}/**/*.apk
```

**Why this matters:** This is the first link in the chain. Without the symbols zip being
generated during the build, steps 2 and 3 are irrelevant.

---

### Step 2 — Clear `USYMTOOL_PATH_OVERRIDE` so Android uses the correct binary (Bug 2)

**File:** `usymtool.sh`

`USYMTOOL_PATH_OVERRIDE` is set to a hardcoded local developer path pointing to
`Contents/Tools/macosx/usymtool`. Because it is not empty, it overrides the Android
platform default (`Contents/Helpers/usymtoolarm64`). This override is never replaced
by any `sed` command in the workflow.

On GitHub Actions `macos-latest` runners (Apple Silicon since 2024), the ARM64 binary
in `Helpers/` is required. The binary in `Tools/macosx/` is either the wrong architecture
or a different tool variant.

**Change:** Clear the override in `usymtool.sh` so the Android `case` block picks the
correct default.

```bash
# BEFORE
USYMTOOL_PATH_OVERRIDE="/Applications/Unity/Hub/Editor/6000.2.7f2/Unity.app/Contents/Tools/macosx/usymtool"

# AFTER
USYMTOOL_PATH_OVERRIDE=""
```

**Why this matters:** Invoking the wrong binary will either crash silently or produce
corrupt symbol packages even if symbol files are present.

---

### Step 3 — Fix `IL2CPP_FILE_ROOT_OVERRIDE` to point to the actual IL2CPP output (Bug 3)

This is the direct cause of missing C# line numbers.

`usymtool` uses the `-il2cppFileRoot` parameter to locate the IL2CPP-generated `.cpp`
source files. These files contain the mapping between native addresses and C# source
locations. When the path is wrong, usymtool cannot build the C# mapping and line numbers
are absent from all stack traces.

**Change A — `usymtool.sh`:** Clear the in-script override so it does not fight with
the value set by the workflow.

```bash
# BEFORE
IL2CPP_FILE_ROOT_OVERRIDE="."

# AFTER
IL2CPP_FILE_ROOT_OVERRIDE=""
```

**Change B — `symbol-upload.yml`:** Replace the `export IL2CPP_FILE_ROOT_OVERRIDE="."`
line with the real path inside the Android build artifacts.

```yaml
# BEFORE
# Fix documented for Unity 6: missing C# line numbers
export IL2CPP_FILE_ROOT_OVERRIDE="."

# AFTER
export IL2CPP_FILE_ROOT_OVERRIDE="${{ github.workspace }}/Library/Bee/artifacts/Android/il2cppOutput/cpp"
```

**Why this matters:** This is the proximate cause of the bug reported by the user.
Even with Steps 1 and 2 fixed, line numbers will not appear until usymtool can find
the IL2CPP source files.

**Note on the "Unity 6 documented fix" comment:** Setting `IL2CPP_FILE_ROOT_OVERRIDE`
to `"."` is not a documented Unity 6 fix — it is a misconfiguration. The correct Unity 6
workaround for missing C# line numbers is ensuring the `Library/Bee/` path is available
at upload time and passing it explicitly.

---

### Step 4 — Fix `LZMA_PATH` to use the resolved editor path (Bug 5)

**File:** `usymtool.sh`

`LZMA_PATH` is hardcoded to a local developer path. On CI runners this path may not
exist, causing the upload to fail silently or produce uncompressed output.

**Change:** Replace the hardcoded path with one derived from `UNITY_EDITOR_PATH`,
which is already correctly set by the workflow's `sed` replacement.

```bash
# BEFORE
export LZMA_PATH="/Applications/Unity/Hub/Editor/6000.2.7f2/Unity.app/Contents/Tools/macosx/lzma"

# AFTER
export LZMA_PATH="${UNITY_EDITOR_PATH}/Unity.app/Contents/Tools/macosx/lzma"
```

---

### Step 5 — (Optional but recommended) Create the Android CI Build Profile (Bug 4)

**Directory:** `Assets/Settings/Build Profiles/`

The workflow contains a commented-out line referencing `Android_CI.asset`:

```yaml
# customParameters: -buildProfile "Assets/Settings/Build Profiles/Android_CI.asset"
```

This file does not exist. While the `-createSymbols` flag added in Step 1 handles symbol
generation without a profile, a dedicated CI build profile gives you explicit control over:
- Scripting backend (enforce IL2CPP)
- Debug symbol type (set to **Public** for upload compatibility)
- Stripping level
- Other Android-specific settings that differ between dev and CI builds

**How to create it:**
1. Open the project in Unity Editor 6000.2.7f2
2. Go to **File → Build Profiles**
3. Create a new Android profile, name it `Android_CI`
4. Set **Debug Symbols** to `Public`
5. Save — the asset will appear at `Assets/Settings/Build Profiles/Android_CI.asset`
6. Uncomment the `customParameters` line in the workflow and remove the standalone
   `-createSymbols` flag (the profile will handle it)

---

## Verification Checklist

After applying all fixes, confirm the following in the next CI run:

- [ ] Build step logs show `Creating symbols package` or similar Unity output
- [ ] A `.symbols.zip` file is produced alongside the APK in `Build/`
- [ ] Symbol upload step exits with code 0
- [ ] Unity Cloud Diagnostics dashboard shows the build version under **Symbol Files**
- [ ] A test crash on a device produces a stack trace with C# file names and line numbers

---

## Files to Modify

| File | Steps |
|------|-------|
| `.github/workflows/symbol-upload.yml` | Step 1, Step 3B |
| `usymtool.sh` | Step 2, Step 3A, Step 4 |
| `Assets/Settings/Build Profiles/Android_CI.asset` | Step 5 (create in Editor) |
