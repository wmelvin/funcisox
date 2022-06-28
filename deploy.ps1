# ----------------------------------------------------------------------
# PowerShell script with steps to deploy using the Azure CLI.
#
# This script is intended to be executed in selected sections (F8 in IDE
# or paste into CLI), not all at once.
#
# ----------------------------------------------------------------------

# az login

# az account set -s $SUBSCRIPTION

# -- Assign vars for script.
$baseName = "func01"
$rgName = "${baseName}-rg"
$location = "eastus"
$funcAppName = "funcisox"
$storageAcctName = "${baseName}storage"
$appInsightsName = "${baseName}insights"


# -- Create the Resource Group.
az group create -n $rgName -l $location


# -- Create the Storage Account.
#    https://docs.microsoft.com/en-us/cli/azure/storage/account?view=azure-cli-latest#az-storage-account-create

az storage account create -n $storageAcctName -l $location -g $rgName --sku Standard_LRS


# -- Create the Application Insights resource.
#    https://docs.microsoft.com/en-us/cli/azure/resource?view=azure-cli-latest#az-resource-create

az resource create -n $appInsightsName -g $rgName `
  --resource-type "Microsoft.Insights/components" `
  --properties '{\"Application_Type\":\"web\"}'


# -- Create the Azure Functions app.
#    https://docs.microsoft.com/en-us/cli/azure/functionapp?view=azure-cli-latest#az-functionapp-create

az functionapp create -n $funcAppName -g $rgName `
  --functions-version 3 `
  --storage-account $storageAcctName `
  --consumption-plan-location $location `
  --app-insights $appInsightsName `
  --runtime dotnet `
  --os-type Windows


# TODO: Settings...
# Source settings vars from script in local (encrypted) folder.
#
# -- Apply settings.
# az functionapp config appsettings set -n $funcAppName -g $rgName `
#   --settings "Setting1=$setting1" "Setting2=$setting2"  #... etc.


# -- List resources.
# az resource list -g $rgName -o table


# -- List settings.
# az functionapp config appsettings list -n $funcAppName -g $rgName -o table


# -- Publish the Application.
#    https://docs.microsoft.com/en-us/azure/azure-functions/functions-core-tools-reference?tabs=v2#func-azure-functionapp-publish

func azure functionapp publish $funcAppName


# -- Delete the whole lot when done.
# az group delete -n $rgName
