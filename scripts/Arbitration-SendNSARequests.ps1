# Arbitration Script for Sending NSA Requests 
Function SendNSARequests {
Param (
[Parameter (Mandatory=$true)][string]$ClientId, 
[Parameter (Mandatory=$true)][string]$Resource, 
[Parameter (Mandatory=$true)][string]$TenantId
)
    $token = GetAccessToken -ClientId $ClientId -Resource $Resource -TenantId $TenantId
    
    $cases = GetNSAClaims -TenantId $TenantId -Token $token

    Write-Host $cases

}

Function GetNSAClaims {
Param (
[Parameter (Mandatory=$true)][string]$Token,
[Parameter (Mandatory=$true)][string]$TenantId
)

    $baseUrl = If ($TenantId -eq '2e09f3a3-0520-461f-8474-052a8ed7814a') {"https://localhost:44473"} ElseIf ($TenantId -eq 'production id here') {""} Else {""}
    If ($baseUrl -eq '' -or $Token -eq '') {
        Write-Error 'Bad TenantId or Missing Token'
        throw
    }
    Write-Host "Fetching Claims from $baseUrl ..."
    $headers = @{'Authorization'='Bearer '+$Token}
    $rptUrl = "$baseUrl/api/cases/needsnsarequest"
    try {
        $result = Invoke-RestMethod -Uri $rptUrl -Method Get -Headers $headers
        return $result
    } catch {
        Write-Host $_
    }
}

Function Get-FirstEnvironmentVariableValue {
Param (
[Parameter (Mandatory=$true)][string[]]$Names
)
    foreach ($name in $Names) {
        foreach ($scope in @('Process', 'User', 'Machine')) {
            $value = [System.Environment]::GetEnvironmentVariable($name, $scope)
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                Write-Verbose "Using client secret value from environment variable '$name'."
                return $value
            }
        }
    }

    return $null
}

Function Get-ClientSecret {
Param (
[Parameter (Mandatory=$true)][string]$TenantId
)
    $normalizedTenantId = ($TenantId -replace '[^a-zA-Z0-9]', '').ToUpper()

    $environmentVariableCandidates = @()
    if (-not [string]::IsNullOrWhiteSpace($normalizedTenantId)) {
        $environmentVariableCandidates += "ARBITRATION_CLIENT_SECRET__${normalizedTenantId}"
    }
    $environmentVariableCandidates += 'ARBITRATION_CLIENT_SECRET'

    $secret = Get-FirstEnvironmentVariableValue -Names $environmentVariableCandidates
    if (-not [string]::IsNullOrWhiteSpace($secret)) {
        return $secret
    }

    $vaultNameCandidates = @()
    $secretNameCandidates = @()
    if (-not [string]::IsNullOrWhiteSpace($normalizedTenantId)) {
        $vaultNameCandidates += "ARBITRATION_CLIENT_SECRET_VAULT_NAME__${normalizedTenantId}"
        $secretNameCandidates += "ARBITRATION_CLIENT_SECRET_SECRET_NAME__${normalizedTenantId}"
    }
    $vaultNameCandidates += 'ARBITRATION_CLIENT_SECRET_VAULT_NAME'
    $secretNameCandidates += 'ARBITRATION_CLIENT_SECRET_SECRET_NAME'

    $vaultName = Get-FirstEnvironmentVariableValue -Names $vaultNameCandidates
    $secretName = Get-FirstEnvironmentVariableValue -Names $secretNameCandidates

    if (-not [string]::IsNullOrWhiteSpace($vaultName) -and -not [string]::IsNullOrWhiteSpace($secretName)) {
        try {
            if (-not (Get-Module -Name Az.KeyVault)) {
                Import-Module Az.KeyVault -ErrorAction Stop | Out-Null
            }

            $secret = Get-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -AsPlainText -ErrorAction Stop
            Write-Verbose "Retrieved client secret from Azure Key Vault '$vaultName' using secret '$secretName'."
            return $secret
        }
        catch {
            Write-Error "Failed to retrieve secret '$secretName' from Key Vault '$vaultName'. $_"
            throw
        }
    }

    return $null
}

Function GetAccessToken {
Param (
[Parameter (Mandatory=$true)][string]$ClientId,
[Parameter (Mandatory=$true)][string]$Resource,
[Parameter (Mandatory=$true)][string]$TenantId
)
    $secret = Get-ClientSecret -TenantId $TenantId
    if ([string]::IsNullOrWhiteSpace($secret)) {
        Write-Error "Client secret not found. Set 'ARBITRATION_CLIENT_SECRET' (or the tenant-specific variant) or configure the Azure Key Vault environment variables."
        throw
    }

    try {
        # make service call
        Clear-Host

        $url="https://login.microsoftonline.com/$TenantId/oauth2/token"
        $hdrs = @{}
        #$hdrs.Add("Content-Type","application/x-www-form-urlencoded")
        $hdrs.Add("Accept","*/*")
        $hdrs.Add("Accept-Encoding","gzip, deflate, br")
        $body = @{grant_type='client_credentials';client_id=$ClientId;client_secret=$secret;resource=$Resource;tenant_id=$TenantId}

        Write-Host "Getting Access Token: $url ..."

        $result = Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType 'application/x-www-form-urlencoded' -Headers $hdrs

        # return the token
        return $result.access_token
    }
    catch {
        Write-Host $_
    }
}
