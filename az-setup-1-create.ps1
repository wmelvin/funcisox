# ----------------------------------------------------------------------
#  az-setup-1-create.ps1
#
#  Create resources for the "FunciSox" Azure Functions app using the
#  Azure CLI.
#
#  2023-02-03: Was deploy.ps1. Refactored to use az-setup-1.init.ps1.
# ----------------------------------------------------------------------

# az login

# az account set -s $SUBSCRIPTION

# ======================================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# -- Source the initialization script.
. ./az-setup-0-init.ps1


# - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
# Create resources:

# -- Create the Resource Group.
az group create -n $rgName -l $location


# -- Create the Storage Account.
#    https://docs.microsoft.com/en-us/cli/azure/storage/account?view=azure-cli-latest#az-storage-account-create

az storage account create -n $storageAcctName -l $location -g $rgName --sku Standard_LRS


# -- Get the storage account key.
#    Example found in Microsoft Docs: "Mount a file share to a Python function app - Azure CLI"
#    https://docs.microsoft.com/en-us/azure/azure-functions/scripts/functions-cli-mount-files-storage-linux

$storageKey = $(az storage account keys list -g $rgName -n $storageAcctName --query '[0].value' -o tsv)


# -- Create storage containers.
#    https://docs.microsoft.com/en-us/cli/azure/storage/container?view=azure-cli-latest#az-storage-container-create

az storage container create --account-key $storageKey --account-name $storageAcctName -n "funcisox"
az storage container create --account-key $storageKey --account-name $storageAcctName -n "funcisox-in"
az storage container create --account-key $storageKey --account-name $storageAcctName -n "funcisox-out"


# -- Create the Application Insights resource.
#    https://docs.microsoft.com/en-us/cli/azure/resource?view=azure-cli-latest#az-resource-create

az resource create -n $appInsightsName -g $rgName `
  --resource-type "Microsoft.Insights/components" `
  --properties '{\"Application_Type\":\"web\"}'


if ($USE_APP_SERVICE_PLAN) 
{
  # -- Create the Azure App Service Plan.
  #    https://docs.microsoft.com/en-us/cli/azure/functionapp/plan?view=azure-cli-latest#az-functionapp-plan-create

  az functionapp plan create -n $funcPlanName -g $rgName `
    --location "$location" `
    --sku $planSku

  # -- Create the Azure Functions app using App Service Plan.
  #    https://docs.microsoft.com/en-us/cli/azure/functionapp?view=azure-cli-latest#az-functionapp-create

  az functionapp create -n $funcAppName -g $rgName `
    --functions-version 4 `
    --storage-account $storageAcctName `
    --plan $funcPlanName `
    --app-insights $appInsightsName `
    --runtime dotnet `
    --os-type Windows
}
else 
{
  # -- Create the Azure Functions app using Consumption Plan.
  #    https://docs.microsoft.com/en-us/cli/azure/functionapp?view=azure-cli-latest#az-functionapp-create

  $funcPlanName = ""

  az functionapp create -n $funcAppName -g $rgName `
    --functions-version 4 `
    --storage-account $storageAcctName `
    --consumption-plan-location $location `
    --app-insights $appInsightsName `
    --runtime dotnet `
    --os-type Windows
}



# -- Apply settings.
#    https://docs.microsoft.com/en-us/cli/azure/functionapp/config/appsettings?view=azure-cli-latest#az-functionapp-config-appsettings-set

az functionapp config appsettings set -n $funcAppName -g $rgName `
  --settings 'WavFasterTempos="1.09,1.18"' 'DownloadTimeout="3h"' `
    "SendGridKey=$SGKey" `
    "EmailRecipientAddress=$EmailRecipientAddress" `
    "EmailSenderAddress=$EmailSenderAddress" `
    "PreserveTempFiles=$preserveTempFiles"

#  az functionapp config appsettings set -n $funcAppName -g $rgName --settings "PreserveTempFiles=True"


# - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
# Additional commands and information:

# -- List resources.
# az resource list -g $rgName -o table


# -- List settings.
# az functionapp config appsettings list -n $funcAppName -g $rgName -o table


# -- Publish the Application.
#    https://docs.microsoft.com/en-us/azure/azure-functions/functions-core-tools-reference?tabs=v2#func-azure-functionapp-publish
#
#    (Been using Visual Studio to publish so far.)
#
# func azure functionapp publish $funcAppName


# -- Delete the whole lot when done.
# az group delete -n $rgName


# ----------------------------------------------------------------------
