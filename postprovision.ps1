$lines=((azd env get-values) -split "`n")

cd .\Backend

dotnet user-secrets clear 

foreach ($line in $lines) {
    $name, $value = $line -split '='
    $value = ($value -replace '"', '')
    $name = ($name -replace '__', ':')
    if($value -ne '') {
        $command = "dotnet user-secrets set $name $value"
        Invoke-Expression $command
    }
}

cd ..\Frontend

dotnet user-secrets clear 

foreach ($line in $lines) {
    $name, $value = $line -split '='
    $value = ($value -replace '"', '')
    $name = ($name -replace '__', ':')
    if($value -ne '') {
        $command = "dotnet user-secrets set $name $value"
        Invoke-Expression $command
    }
}

cd ..