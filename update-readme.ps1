Push-Location $PSScriptRoot

"= TIL - Today I Learned" > README.asc
"" >> README.asc
"A collection of small tips and tricks inspired by https://simonwillison.net/2020/Apr/20/self-rewriting-readme/[this post in Simon Willison’s Weblog]" >> README.asc
"" >> README.asc


$dirs = Get-ChildItem -Path . -Directory -Exclude ".github"
foreach($dir in $dirs) {
    $files = Get-ChildItem -Path $dir.FullName -Filter *.asc | Sort-Object -Property Name

    $count = ($files | Measure-Object).Count
    if($found -eq 0) {
        continue
    }

    "= $($dir.Name)" >> README.asc
    "" >> README.asc

    foreach($file in $files) {
        "* $($file.Name)" >> README.asc
    }

    "" >> README.asc
}

Pop-Location
