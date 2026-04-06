# VAH.Backend.Tests Setup Script
# This PowerShell script will organize the test project files into their proper directory structure

Write-Host "Creating VAH.Backend.Tests directory structure..." -ForegroundColor Green

# Create main directories
New-Item -ItemType Directory -Path "VAH.Backend.Tests" -Force | Out-Null
New-Item -ItemType Directory -Path "VAH.Backend.Tests\Fixtures" -Force | Out-Null
New-Item -ItemType Directory -Path "VAH.Backend.Tests\Builders" -Force | Out-Null
New-Item -ItemType Directory -Path "VAH.Backend.Tests\Unit" -Force | Out-Null
New-Item -ItemType Directory -Path "VAH.Backend.Tests\Integration" -Force | Out-Null
New-Item -ItemType Directory -Path "VAH.Backend.Tests\Properties" -Force | Out-Null

Write-Host "Moving files to proper locations..." -ForegroundColor Yellow

# Move the main project file (already in correct location)
Move-Item "VAH.Backend.Tests.csproj" "VAH.Backend.Tests\" -Force

# Move the GlobalUsings.cs
Move-Item "GlobalUsings.cs" "VAH.Backend.Tests\" -Force

# Move fixture files
Move-Item "TestFixture.cs" "VAH.Backend.Tests\Fixtures\" -Force

# Move builder files
Move-Item "ServiceBuilder.cs" "VAH.Backend.Tests\Builders\" -Force
Move-Item "AssetBuilder.cs" "VAH.Backend.Tests\Builders\" -Force
Move-Item "CollectionBuilder.cs" "VAH.Backend.Tests\Builders\" -Force
Move-Item "UserBuilder.cs" "VAH.Backend.Tests\Builders\" -Force

# Move properties files
Move-Item "launchSettings.json" "VAH.Backend.Tests\Properties\" -Force

# Create .gitkeep files in appropriate directories
New-Item -ItemType File -Path "VAH.Backend.Tests\Unit\.gitkeep" -Force | Out-Null
New-Item -ItemType File -Path "VAH.Backend.Tests\Integration\.gitkeep" -Force | Out-Null

# Clean up temporary files
Remove-Item "unit-gitkeep.txt" -Force -ErrorAction SilentlyContinue
Remove-Item "integration-gitkeep.txt" -Force -ErrorAction SilentlyContinue
Remove-Item "create_dirs.bat" -Force -ErrorAction SilentlyContinue
Remove-Item "create_dirs.ps1" -Force -ErrorAction SilentlyContinue

Write-Host "VAH.Backend.Tests project structure created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Directory Structure:" -ForegroundColor Cyan
Write-Host "VAH.Backend.Tests/"
Write-Host "├── VAH.Backend.Tests.csproj"
Write-Host "├── GlobalUsings.cs"
Write-Host "├── Fixtures/"
Write-Host "│   └── TestFixture.cs"
Write-Host "├── Builders/"
Write-Host "│   ├── ServiceBuilder.cs"
Write-Host "│   ├── AssetBuilder.cs"
Write-Host "│   ├── CollectionBuilder.cs"
Write-Host "│   └── UserBuilder.cs"
Write-Host "├── Unit/"
Write-Host "│   └── .gitkeep"
Write-Host "├── Integration/"
Write-Host "│   └── .gitkeep"
Write-Host "└── Properties/"
Write-Host "    └── launchSettings.json"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "1. Run 'dotnet restore' to install NuGet packages"
Write-Host "2. Add the project to your solution: dotnet sln add VAH.Backend.Tests/VAH.Backend.Tests.csproj"
Write-Host "3. Start writing your unit and integration tests!"