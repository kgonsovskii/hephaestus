function Generate-RandomString {
    param (
        [int]$length = 10
    )
    $chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    -join (Get-Random -Count $length -InputObject $chars.ToCharArray())
}

function Generate-RandomCode {
    $codeLines = @()
    $functionNames = @()
    $variables = @()
    $actions = @("writedbg2")
    $messages = @()

    $five = 5
    $twenty = 20

    # Generate random function and variable names
    for ($i = 0; $i -lt $five; $i++) {
        $functionNames += "fn_$(Generate-RandomString)"
        $variables += "`$var_$(Generate-RandomString)"
    }

    # Generate random messages
    for ($i = 0; $i -lt $twenty; $i++) {
        $messages += "$(Generate-RandomString 15)"
    }

    # Generate a random name for the logging function
    $logFunctionName = "fn_Log_$(Generate-RandomString 10)"

    # Add the log function definition once
    $codeLines += "function $logFunctionName {"
    $codeLines += "    param ("
    $codeLines += "        [string]`$message"
    $codeLines += "    )"
    $codeLines += "    writedbg2 'Log: `$message'"
    $codeLines += "}"

    # Generate random functions
    for ($i = 0; $i -lt $five; $i++) {
        $funcName = $functionNames[$i]
        $var1 = $variables[$i]
        $var2 = $variables[($i + 1) % $five]
        $codeLines += "function $funcName {"
        $codeLines += "    param ("
        $codeLines += "        [int]$var1,"
        $codeLines += "        [int]$var2"
        $codeLines += "    )"
        $codeLines += "    `$result = $var1 + $var2"
        $codeLines += "    return `$result"
        $codeLines += "}"
    }

    # Generate random if-else statements
    for ($i = 0; $i -lt $five; $i++) {
        $var = $variables[$i]
        $condition = "$var -gt $(Get-Random -Minimum 1 -Maximum $twenty)"
        $codeLines += "if ($condition) {"
        $codeLines += "    $($actions | Get-Random) '$(Generate-RandomString 15)'"
        $codeLines += "} else {"
        $codeLines += "    $($actions | Get-Random) '$(Generate-RandomString 15)'"
        $codeLines += "}"
    }

    # Add function calls and random variable assignments
    for ($i = 0; $i -lt $twenty; $i++) {
        $var1 = $variables | Get-Random
        $var2 = $variables | Get-Random
        $funcName = $functionNames | Get-Random
        $message = $messages | Get-Random
        $action = $actions | Get-Random

        if ($action -eq "writedbg2" -or $action -eq "writedbg2") {
            $codeLines += "$var1 = $funcName -param1 $(Get-Random -Minimum 1 -Maximum 100) -param2 $(Get-Random -Minimum 1 -Maximum 100)"
            $codeLines += "$action '$(Generate-RandomString 15)'"
        } else {
            $codeLines += "$logFunctionName '$(Generate-RandomString 15)'"
        }
    }

    # Return generated code as joined string
    $codeLines += " "
    return $codeLines -join "`r`n"
}

# # Generate random code
# $randomCode = Generate-RandomCode

# # Determine the path to save the file
# $scriptPath = $PSScriptRoot
# $filePath = Join-Path -Path $scriptPath -ChildPath "random.ps1"

# # Save the generated code to random.ps1
# $randomCode | Set-Content -Path $filePath -Encoding UTF8

# # Output the file path
# Write-Output "Generated code saved to: $filePath"
