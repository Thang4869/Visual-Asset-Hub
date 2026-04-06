@echo off
echo Creating VAH.Backend.Tests directory structure...

REM Create main directories
mkdir "VAH.Backend.Tests" 2>nul
mkdir "VAH.Backend.Tests\Fixtures" 2>nul
mkdir "VAH.Backend.Tests\Builders" 2>nul
mkdir "VAH.Backend.Tests\Unit" 2>nul
mkdir "VAH.Backend.Tests\Integration" 2>nul
mkdir "VAH.Backend.Tests\Properties" 2>nul

echo Moving files to proper locations...

REM Move the main project file
move "VAH.Backend.Tests.csproj" "VAH.Backend.Tests\" >nul

REM Move the GlobalUsings.cs
move "GlobalUsings.cs" "VAH.Backend.Tests\" >nul

REM Move fixture files
move "TestFixture.cs" "VAH.Backend.Tests\Fixtures\" >nul

REM Move builder files
move "ServiceBuilder.cs" "VAH.Backend.Tests\Builders\" >nul
move "AssetBuilder.cs" "VAH.Backend.Tests\Builders\" >nul
move "CollectionBuilder.cs" "VAH.Backend.Tests\Builders\" >nul
move "UserBuilder.cs" "VAH.Backend.Tests\Builders\" >nul

REM Move properties files
move "launchSettings.json" "VAH.Backend.Tests\Properties\" >nul

REM Create .gitkeep files
echo. > "VAH.Backend.Tests\Unit\.gitkeep"
echo. > "VAH.Backend.Tests\Integration\.gitkeep"

REM Clean up temporary files
del "unit-gitkeep.txt" 2>nul
del "integration-gitkeep.txt" 2>nul
del "create_dirs.bat" 2>nul
del "create_dirs.ps1" 2>nul

echo.
echo VAH.Backend.Tests project structure created successfully!
echo.
echo Directory Structure:
echo VAH.Backend.Tests/
echo ├── VAH.Backend.Tests.csproj
echo ├── GlobalUsings.cs
echo ├── Fixtures/
echo │   └── TestFixture.cs
echo ├── Builders/
echo │   ├── ServiceBuilder.cs
echo │   ├── AssetBuilder.cs
echo │   ├── CollectionBuilder.cs
echo │   └── UserBuilder.cs
echo ├── Unit/
echo │   └── .gitkeep
echo ├── Integration/
echo │   └── .gitkeep
echo └── Properties/
echo     └── launchSettings.json
echo.
echo Next steps:
echo 1. Run 'dotnet restore' to install NuGet packages
echo 2. Add the project to your solution: dotnet sln add VAH.Backend.Tests/VAH.Backend.Tests.csproj
echo 3. Start writing your unit and integration tests!
echo.
pause