# ======================================================================
#  az-setup-2-fileup-app.ps1
#  
#  Add the file upload web app (flask-fileup-az) to the FunciSox 
#  deployment.
#
#  Running this script will:
#  - Create an App Service Plan (Linux).
#  - Create a Web App.
#  - Configure the Web App for ZIP file deployment.
#
# ----------------------------------------------------------------------

# az login

# az account set -s $SUBSCRIPTION

# ======================================================================

# -- Source the initialization script.
. ./az-setup-0-init.ps1

# -- Check that the variables required by the file upload app were set.
function CheckVarSet ([string] $varName) {

  if (![bool](Get-Variable -Name $varName -ErrorAction:Ignore)) {
    Yell "ERROR: '$varName' not set in '$keysFile'."
    Exit 1
  }
}

CheckVarSet "fileupSettings"


# -- Check that required settings (dictionary keys) exist.
function CheckKeyExists ([string] $varName) {

  if (! $fileupSettings.ContainsKey($varName)) {
    Yell "ERROR: '$varName' not set in '$keysFile'."
    Exit 1
  }
}

CheckKeyExists "FILEUP_SECRET_KEY"
CheckKeyExists "FILEUP_MAX_UPLOAD_MB"
CheckKeyExists "FILEUP_ENABLE_FEATURES"
CheckKeyExists "FILEUP_UPLOAD_ACCEPT"
CheckKeyExists "FILEUP_MSAL_REDIRECT_PATH"
CheckKeyExists "FILEUP_MSAL_AUTHORITY"
CheckKeyExists "FILEUP_MSAL_CLIENT_ID"
CheckKeyExists "FILEUP_MSAL_CLIENT_SECRET"
CheckKeyExists "FILEUP_MSAL_SCOPE"
CheckKeyExists "FILEUP_STORAGE_CONTAINER"
CheckKeyExists "FILEUP_STORAGE_TABLE"
CheckKeyExists "FILEUP_STORAGE_ACCOUNT_NAME"
CheckKeyExists "FILEUP_STORAGE_ACCOUNT_KEY"
CheckKeyExists "FILEUP_STORAGE_ENDPOINT_SUFFIX"


#  Build a settings string, to use in 'az webapp config appsettings', using the
#  key=value pairs in the $fileupSettings dictionary loaded from $keysFile.

$appSettingsStr = ""
foreach ($key in $fileupSettings.Keys) {
  $value = $fileupSettings[$key]
  if (0 -lt $value.Length) {
    $appSettingsStr += (' "' + $key + '=' + $value + '"')
  }
}

#  Run this to see that the string has spaces in the right places.
# $appSettingsStr.Split(" ")



# ======================================================================
# Create and configure Azure resources.

# if (RGExists($rgName)) {
#   Say "`nResource group exists: $rgName`n"
# }
# else {
#   Say "`nSTEP - Create resource group: $rgName`n"
#   az group create -n $rgName -l $location
# }


# -- Create the App Service Plan (Linux).
#    https://docs.microsoft.com/en-us/cli/azure/appservice/plan?view=azure-cli-latest#az-appservice-plan-create

Say "`nSTEP - Create App Service Plan: $appServiceName`n"

az appservice plan create `
  --name $appServiceName `
  --resource-group $rgName `
  --is-linux `
  --sku s1


# -- Create the Web App.
#    https://docs.microsoft.com/en-us/cli/azure/webapp?view=azure-cli-latest#az-webapp-create
#
#    az webapp list-runtimes

Say "`nSTEP - Create Web App: $webAppName`n"

az webapp create `
  -g $rgName `
  -p $appServiceName `
  --name $webAppName `
  --runtime "PYTHON:3.10"


# -- Configure for ZIP file deployment.
#    https://learn.microsoft.com/en-us/azure/app-service/quickstart-python?tabs=flask%2Cwindows%2Cazure-cli%2Czip-deploy%2Cdeploy-instructions-azcli%2Cterminal-bash%2Cdeploy-instructions-zip-azcli#3---deploy-your-application-code-to-azure

Say "`nSTEP - Configure settings for: $webAppName`n"

az webapp config appsettings set `
    -g $rgName `
    --name $webAppName `
    --settings SCM_DO_BUILD_DURING_DEPLOYMENT=true


# -- Enable logging to filesystem.
#    https://learn.microsoft.com/en-us/cli/azure/webapp/log?view=azure-cli-latest#az-webapp-log-config

#  Method 1:
# az webapp log config `
#   -g $rgName `
#   --name $webAppName `
#   --web-server-logging filesystem

#  Method 2:
#  Use resource properties to enable logging and set the log quota (MB).

$webappResourceId = (az webapp show -g $rgName -n $webAppName --query id)

#  The properties to set are on the <webapp>/config/web resource.
$webConfigResource = "${webappResourceId}/config/web"

# az resource show --ids $webConfigResource
az resource update --ids $webConfigResource --set properties.httpLoggingEnabled=true
az resource update --ids $webConfigResource --set properties.logsDirectorySizeLimit=44



# -- Configure settings for the web app. These are available to the app as environment variables.
#    https://learn.microsoft.com/en-us/cli/azure/webapp/config/appsettings?view=azure-cli-latest

Say "`nSTEP - Configure web app settings for: $webAppName`n"

#  In order to treat the settings in $appSettingsStr as separate arguments that
#  follow '--settings', create the az command as an expression and invoke it.

$expr = "az webapp config appsettings set -g $rgName --name $webAppName --settings $appSettingsStr"
Invoke-Expression $expr


# -- Set custom startup command for running the Flask app.
#    https://learn.microsoft.com/en-us/azure/app-service/configure-language-python#customize-startup-command

Say "`nSTEP - Configure startup command for: $webAppName`n"

$startCmd = "gunicorn --bind=0.0.0.0 --timeout 600 --chdir fileup_app fileup:app"
az webapp config set -g $rgName --name $webAppName --startup-file $startCmd



# ----------------------------------------------------------------------
# Additional commands and information.


# -- List resources.
#
# az resource list -g $rgName -o table


# ======================================================================
