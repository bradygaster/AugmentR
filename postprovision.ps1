function Set-DotnetUserSecrets {
    param ($path, $lines)
    Push-Location
    cd $path
    dotnet user-secrets init
    dotnet user-secrets clear
    foreach ($line in $lines) {
        # Split the line at the first equal sign only
        $parts = $line -split '=', 2
        $name = $parts[0]
        $value = $parts[1]

        # Remove quotes from the value
        $value = $value -replace '"', ''
        
        # Replace double underscores with colon in the name
        $name = $name -replace '__', ':'

        # Set the secret if the value is not empty
        if ($value -ne '') {
            dotnet user-secrets set $name $value | Out-Null
        }
    }
    Pop-Location
}

$lines = (azd env get-values) -split "`n"
Set-DotnetUserSecrets -path ".\Backend" -lines $lines
Set-DotnetUserSecrets -path ".\Frontend" -lines $lines