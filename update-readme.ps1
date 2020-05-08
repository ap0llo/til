Push-Location $PSScriptRoot

# README.asc header

"= TIL - Today I Learned" > README.asc
"" >> README.asc
"A collection of small tips and tricks inspired by https://simonwillison.net/2020/Apr/20/self-rewriting-readme/[this post in Simon Willison’s Weblog]" >> README.asc
"" >> README.asc

# For directory in the repository, add a section to the README file

$dirs = Get-ChildItem -Path . -Directory -Exclude ".github"
foreach ($dir in $dirs) {

    # Get .asc files from the directory, order by name
    $files = Get-ChildItem -Path $dir.FullName -Filter *.asc | Sort-Object -Property Name

    # Skip directories without .asc files
    $count = ($files | Measure-Object).Count
    if ($count -eq 0) {
        continue
    }

    # Determine the header of the section
    # By default, use the directory name
    $header = $dir.Name

    # If there is a directory.json file in the directory
    # load the header from there
    $dirInfoPath = Join-Path $dir.FullName "directory.json"
    if (Test-Path $dirInfoPath) {
        $directoryInfo = Get-Content -Path $dirInfoPath -Raw | ConvertFrom-Json
        if ($directoryInfo.title) {
            $header = $directoryInfo.title 
        }
    }

    "== $header" >> README.asc
    "" >> README.asc

    # Add list of files in the directory
    foreach ($file in $files) {

        # Get the date the file was last modified
        $command = "git log -1 --pretty=`"format:%cs`" `"$($file.FullName)`""
        $date = Invoke-Expression $command
        if ($LASTEXITCODE -ne 0) {
            throw "Command '$command' failed with exit code $LASTEXITCODE"
        }
        
        # Get the title from the file (the heading starts with "= ")
        # Use the file name if no title is found
        $title = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        foreach ($line in (Get-Content -Path $file.FullName)) { 
            $line = $line.Trim()
            if ($line.StartsWith("= ")) {
                $title = $line.Substring(2).Trim()
                break
            }
        }
                
        "* link:$($dir.Name)/$($file.Name)[$title] - _$($date)_" >> README.asc
    }

    "" >> README.asc
}

Pop-Location
