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






Function GetAccessToken {
Param (
[Parameter (Mandatory=$true)][string]$ClientId, 
[Parameter (Mandatory=$true)][string]$Resource, 
[Parameter (Mandatory=$true)][string]$TenantId
)
    $secret = If ($TenantId -eq '2e09f3a3-0520-461f-8474-052a8ed7814a') {"Sen8Q~ZHDZ0Br-~2l-hdEfWD7uhPl0OvHz7TYbOG"} ElseIf ($TenantId -eq 'production id here') {""} Else {""}
    If ($secret -eq '') {
        Write-Error 'Unknown TenantId'
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
