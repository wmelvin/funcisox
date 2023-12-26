# ======================================================================
#  az-setup-3-fileup-roles.ps1
#  
#  Add the file upload web app (flask-fileup-az) to the FunciSox 
#  deployment.
#
#  Running this script will:
#  - Create a system-assigned managed identity for the web app.
#  - Assign the "Storage Blob Data Contributor" role to the managed
#    identity.
#
# ----------------------------------------------------------------------

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# -- Source the initialization script.
. ./az-setup-0-init.ps1

# -- Create a system-assigned managed identity for the web app.
#    https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=cli%2Chttp#add-a-system-assigned-identity

Say "`nSTEP - Add system-assigned managed identity to: $webAppName`n"

az webapp identity assign  -g $rgName -n $webAppName


# -- Get the service principal ID for the managed identity.
#    https://learn.microsoft.com/en-us/azure/role-based-access-control/scope-overview
#    https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/howto-assign-access-cli#use-azure-rbac-to-assign-a-managed-identity-access-to-another-resource

#  It appears there is a lag between when the identity is assigned and when it
#  is available to query. Retry a given number of times while $webappIdentityId
#  is empty. Exit with an error if still empty.

$retries = 8
while ($retries -gt 0) {
    $webappIdentityId = "$(az resource list -g $rgName -n $webAppName --query [*].identity.principalId --out tsv)"
    if ($webappIdentityId.Length -gt 0) {
        break
    }
    else {
        $retries--
        Say "Waiting 15 seconds for managed identity to be available..."
        Start-Sleep -Seconds 15
    }
}
if ($webappIdentityId.Length -eq 0) {
    Yell "Failed to get managed identity ID for '$webAppName'."
    Exit 1
}

# -- Get the storage account resource ID.

$storageAcctResId = $(az storage account show -g $rgName -n $storageAcctName --query id --out tsv)


# -- Get the blob resource ID.

$blobResId = "$storageAcctResId/blobServices/default"


# -- Get the ID (name) of the roles to be assigned to the managed identity.
$blobRoleId = (az role definition list --name "Storage Blob Data Contributor" --query [*].name --out tsv)


Say "`nSTEP - Assign blob contributor role to the managed identity.`n"

# -- Add the role assignment to the managed identity.
az role assignment create --assignee $webappIdentityId --role $blobRoleId --scope $blobResId


# ======================================================================
