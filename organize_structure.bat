@echo off
setlocal enabledelayedexpansion
cd /d "b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11"

REM Step 1: Create directories
mkdir "VAH.Backend.Tests" 2>nul
mkdir "VAH.Backend.Tests\Fixtures" 2>nul
mkdir "VAH.Backend.Tests\Builders" 2>nul
mkdir "VAH.Backend.Tests\Unit" 2>nul
mkdir "VAH.Backend.Tests\Integration" 2>nul
mkdir "VAH.Backend.Tests\Properties" 2>nul

echo.
echo --- Creating directories ---
echo ✓ Created: VAH.Backend.Tests
echo ✓ Created: VAH.Backend.Tests\Fixtures
echo ✓ Created: VAH.Backend.Tests\Builders
echo ✓ Created: VAH.Backend.Tests\Unit
echo ✓ Created: VAH.Backend.Tests\Integration
echo ✓ Created: VAH.Backend.Tests\Properties

REM Step 2: Move files
echo.
echo --- Moving files ---

if exist "VAH.Backend.Tests.csproj" (
  move "VAH.Backend.Tests.csproj" "VAH.Backend.Tests\" >nul
  echo ✓ Moved: VAH.Backend.Tests.csproj
) else (
  echo ✗ Not found: VAH.Backend.Tests.csproj
)

if exist "GlobalUsings.cs" (
  move "GlobalUsings.cs" "VAH.Backend.Tests\" >nul
  echo ✓ Moved: GlobalUsings.cs
) else (
  echo ✗ Not found: GlobalUsings.cs
)

if exist "TestFixture.cs" (
  move "TestFixture.cs" "VAH.Backend.Tests\Fixtures\" >nul
  echo ✓ Moved: TestFixture.cs
) else (
  echo ✗ Not found: TestFixture.cs
)

if exist "ServiceBuilder.cs" (
  move "ServiceBuilder.cs" "VAH.Backend.Tests\Builders\" >nul
  echo ✓ Moved: ServiceBuilder.cs
) else (
  echo ✗ Not found: ServiceBuilder.cs
)

if exist "AssetBuilder.cs" (
  move "AssetBuilder.cs" "VAH.Backend.Tests\Builders\" >nul
  echo ✓ Moved: AssetBuilder.cs
) else (
  echo ✗ Not found: AssetBuilder.cs
)

if exist "CollectionBuilder.cs" (
  move "CollectionBuilder.cs" "VAH.Backend.Tests\Builders\" >nul
  echo ✓ Moved: CollectionBuilder.cs
) else (
  echo ✗ Not found: CollectionBuilder.cs
)

if exist "UserBuilder.cs" (
  move "UserBuilder.cs" "VAH.Backend.Tests\Builders\" >nul
  echo ✓ Moved: UserBuilder.cs
) else (
  echo ✗ Not found: UserBuilder.cs
)

if exist "launchSettings.json" (
  move "launchSettings.json" "VAH.Backend.Tests\Properties\" >nul
  echo ✓ Moved: launchSettings.json
) else (
  echo ✗ Not found: launchSettings.json
)

REM Step 3: Create .gitkeep files
echo.
echo --- Creating .gitkeep files ---

type nul > "VAH.Backend.Tests\Unit\.gitkeep"
echo ✓ Created: VAH.Backend.Tests\Unit\.gitkeep

type nul > "VAH.Backend.Tests\Integration\.gitkeep"
echo ✓ Created: VAH.Backend.Tests\Integration\.gitkeep

REM Step 4: Delete cleanup files
echo.
echo --- Deleting cleanup files ---

if exist "unit-gitkeep.txt" (
  del "unit-gitkeep.txt"
  echo ✓ Deleted: unit-gitkeep.txt
) else (
  echo ✗ Not found ^(already deleted?^): unit-gitkeep.txt
)

if exist "integration-gitkeep.txt" (
  del "integration-gitkeep.txt"
  echo ✓ Deleted: integration-gitkeep.txt
) else (
  echo ✗ Not found ^(already deleted?^): integration-gitkeep.txt
)

if exist "create_dirs.bat" (
  del "create_dirs.bat"
  echo ✓ Deleted: create_dirs.bat
) else (
  echo ✗ Not found ^(already deleted?^): create_dirs.bat
)

if exist "create_dirs.ps1" (
  del "create_dirs.ps1"
  echo ✓ Deleted: create_dirs.ps1
) else (
  echo ✗ Not found ^(already deleted?^): create_dirs.ps1
)

echo.
echo ✓ All operations completed successfully!
pause
