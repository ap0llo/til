Push-Location $PSScriptRoot

# README.asc header

"= TIL - Today I Learned" > README.asc
"" >> README.asc
"A collection of small tips and tricks inspired by https://simonwillison.net/2020/Apr/20/self-rewriting-readme/[this post in Simon Willison’s Weblog]" >> README.asc
"" >> README.asc

# For directory in the repository, add a section to the README file

$dirs = Get-ChildItem -Path . -Directory -Exclude ".github"
foreach($dir in $dirs) {

    # Get .asc files from the directory, order by name
    $files = Get-ChildItem -Path $dir.FullName -Filter *.asc | Sort-Object -Property Name

    # Skip directories without .asc files
    $count = ($files | Measure-Object).Count
    if($count -eq 0) {
        continue
    }

    # Use directory name as header
    "== $($dir.Name)" >> README.asc
    "" >> README.asc

    # Add list of files in the directory
    foreach($file in $files) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        "* $name" >> README.asc
    }

    "" >> README.asc
}

Pop-Location
