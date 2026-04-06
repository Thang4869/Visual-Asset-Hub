Dim fso, shell, path, dir

Set fso = CreateObject("Scripting.FileSystemObject")
Set shell = CreateObject("WScript.Shell")

' Change to the working directory
path = "b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11"
shell.CurrentDirectory = path

' Step 1: Create directories
WScript.Echo ""
WScript.Echo "--- Creating directories ---"

CreateDirIfNotExists "VAH.Backend.Tests"
CreateDirIfNotExists "VAH.Backend.Tests\Fixtures"
CreateDirIfNotExists "VAH.Backend.Tests\Builders"
CreateDirIfNotExists "VAH.Backend.Tests\Unit"
CreateDirIfNotExists "VAH.Backend.Tests\Integration"
CreateDirIfNotExists "VAH.Backend.Tests\Properties"

' Step 2: Move files
WScript.Echo ""
WScript.Echo "--- Moving files ---"

MoveFile "VAH.Backend.Tests.csproj", "VAH.Backend.Tests\"
MoveFile "GlobalUsings.cs", "VAH.Backend.Tests\"
MoveFile "TestFixture.cs", "VAH.Backend.Tests\Fixtures\"
MoveFile "ServiceBuilder.cs", "VAH.Backend.Tests\Builders\"
MoveFile "AssetBuilder.cs", "VAH.Backend.Tests\Builders\"
MoveFile "CollectionBuilder.cs", "VAH.Backend.Tests\Builders\"
MoveFile "UserBuilder.cs", "VAH.Backend.Tests\Builders\"
MoveFile "launchSettings.json", "VAH.Backend.Tests\Properties\"

' Step 3: Create .gitkeep files
WScript.Echo ""
WScript.Echo "--- Creating .gitkeep files ---"

CreateEmptyFile "VAH.Backend.Tests\Unit\.gitkeep"
CreateEmptyFile "VAH.Backend.Tests\Integration\.gitkeep"

' Step 4: Delete cleanup files
WScript.Echo ""
WScript.Echo "--- Deleting cleanup files ---"

DeleteFileIfExists "unit-gitkeep.txt"
DeleteFileIfExists "integration-gitkeep.txt"
DeleteFileIfExists "create_dirs.bat"
DeleteFileIfExists "create_dirs.ps1"

WScript.Echo ""
WScript.Echo "✓ All operations completed successfully!"

Sub CreateDirIfNotExists(dirPath)
    Dim fullPath
    fullPath = fso.BuildPath(path, dirPath)
    If Not fso.FolderExists(fullPath) Then
        fso.CreateFolder fullPath
    End If
    WScript.Echo "✓ Created: " & dirPath
End Sub

Sub MoveFile(srcFile, destDir)
    Dim srcPath, destPath
    srcPath = fso.BuildPath(path, srcFile)
    destPath = fso.BuildPath(path, destDir)
    
    If fso.FileExists(srcPath) Then
        fso.MoveFile srcPath, fso.BuildPath(destPath, srcFile)
        WScript.Echo "✓ Moved: " & srcFile & " → " & destDir
    Else
        WScript.Echo "✗ Not found: " & srcFile
    End If
End Sub

Sub CreateEmptyFile(filePath)
    Dim fullPath
    fullPath = fso.BuildPath(path, filePath)
    Dim objFile
    Set objFile = fso.CreateTextFile(fullPath, True)
    objFile.Close
    WScript.Echo "✓ Created: " & filePath
End Sub

Sub DeleteFileIfExists(fileName)
    Dim fullPath
    fullPath = fso.BuildPath(path, fileName)
    If fso.FileExists(fullPath) Then
        fso.DeleteFile fullPath
        WScript.Echo "✓ Deleted: " & fileName
    Else
        WScript.Echo "✗ Not found (already deleted?): " & fileName
    End If
End Sub
