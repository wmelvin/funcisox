# ======================================================================
#  PowerShell script defines functions used in other scripts.


# -- Display a message in green text.
function Say([string]$text)
{
    Write-Host -ForegroundColor Green "$text"
}


# -- Display a message in yellow text.
function Yell([string]$text)
{
    Write-Host -ForegroundColor Yellow "$text"
}


# -- Does a resource group exist?
#    https://docs.microsoft.com/en-us/cli/azure/group?view=azure-cli-latest#az_group_list

function RGExists([string]$rgName)
{
    $t = az group list | ConvertFrom-Json | Select-Object Name
    if ($null -eq $t) {
        return $false
    }
    else {
        return $t.Name.Contains($rgName)
    }
}
