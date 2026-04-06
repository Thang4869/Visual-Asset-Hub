import os
import shutil
from pathlib import Path

# Get the current working directory
cwd = Path.cwd()
print(f'Working directory: {cwd}')

# Step 1: Create directory structure
directories = [
    'VAH.Backend.Tests',
    'VAH.Backend.Tests/Fixtures',
    'VAH.Backend.Tests/Builders',
    'VAH.Backend.Tests/Unit',
    'VAH.Backend.Tests/Integration',
    'VAH.Backend.Tests/Properties'
]

print('\n--- Creating directories ---')
for dir_path in directories:
    full_path = cwd / dir_path
    full_path.mkdir(parents=True, exist_ok=True)
    print(f'✓ Created: {dir_path}')

# Step 2: Move files to their proper locations
files_to_move = {
    'VAH.Backend.Tests.csproj': 'VAH.Backend.Tests/',
    'GlobalUsings.cs': 'VAH.Backend.Tests/',
    'TestFixture.cs': 'VAH.Backend.Tests/Fixtures/',
    'ServiceBuilder.cs': 'VAH.Backend.Tests/Builders/',
    'AssetBuilder.cs': 'VAH.Backend.Tests/Builders/',
    'CollectionBuilder.cs': 'VAH.Backend.Tests/Builders/',
    'UserBuilder.cs': 'VAH.Backend.Tests/Builders/',
    'launchSettings.json': 'VAH.Backend.Tests/Properties/'
}

print('\n--- Moving files ---')
for src_file, dest_dir in files_to_move.items():
    src_path = cwd / src_file
    dest_path = cwd / dest_dir / src_file
    
    if src_path.exists():
        shutil.move(str(src_path), str(dest_path))
        print(f'✓ Moved: {src_file} → {dest_dir}')
    else:
        print(f'✗ Not found: {src_file}')

# Step 3: Create .gitkeep files
print('\n--- Creating .gitkeep files ---')
gitkeep_files = [
    'VAH.Backend.Tests/Unit/.gitkeep',
    'VAH.Backend.Tests/Integration/.gitkeep'
]

for gitkeep_path in gitkeep_files:
    full_path = cwd / gitkeep_path
    full_path.touch()
    print(f'✓ Created: {gitkeep_path}')

# Step 4: Delete cleanup files
print('\n--- Deleting cleanup files ---')
cleanup_files = [
    'unit-gitkeep.txt',
    'integration-gitkeep.txt',
    'create_dirs.bat',
    'create_dirs.ps1'
]

for cleanup_file in cleanup_files:
    file_path = cwd / cleanup_file
    if file_path.exists():
        file_path.unlink()
        print(f'✓ Deleted: {cleanup_file}')
    else:
        print(f'✗ Not found (already deleted?): {cleanup_file}')

print('\n✓ All operations completed successfully!')
