# Azure Functions
## Text Summarization using AI Cognitive Language Service (C#-Isolated)

This sample shows how to take text documents as a input via BlobTrigger, does Text Summarization processing using the AI Congnitive Language service, and then outputs to another text document using BlobOutput binding.  

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=575770869)

## Run on your local environment

### Pre-reqs
1) [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) required *and [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) is strongly recommended*
2) [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=v4%2Cmacos%2Ccsharp%2Cportal%2Cbash#install-the-azure-functions-core-tools)
3) [Azurite](https://github.com/Azure/Azurite)

The easiest way to install Azurite is using a Docker container or the support built into Visual Studio:
```bash
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

4) Once you have your Azure subscription, [create a Language resource](https://portal.azure.com/#create/Microsoft.CognitiveServicesTextAnalytics) in the Azure portal to get your key and endpoint. After it deploys, click Go to resource.  Note: if you perform `azd provision` or `azd up` per the section at the end of the tutorial, this resource will already be created.  
You will need the key and endpoint from the resource you create to connect your application to the API. You'll need to store the key and endpoint into the Env Vars or User Secrets code in a next step the quickstart.
You can use the free pricing tier (Free F0) to try the service, and upgrade later to a paid tier for production.
5) Export these secrets as Env Vars using values from Step 4.

Mac/Linux
```bash
export AI_URL=*Paste from step 4*
export AI_SECRET=*Paste from step 4*
```

Windows

Search for Environment Variables in Settings, create new System Variables similarly to [these instructions](https://docs.oracle.com/en/database/oracle/machine-learning/oml4r/1.5.1/oread/creating-and-modifying-environment-variables-on-windows.html#GUID-DD6F9982-60D5-48F6-8270-A27EC53807D0):

| Variable | Value |
| -------- | ----- |
| AI_URL | *Paste from step 4* |
| AI_SECRET | *Paste from step 4* |
6) [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/) or storage explorer features of [Azure Portal](https://portal.azure.com)
7) Add this `local.settings.json` file to the `./text_summarization` folder to simplify local development.  Optionally fill in the AI_URL and AI_SECRET values per step 4.  This file will be gitignored to protect secrets from committing to your repo.  
```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "AI_URL": "",
        "AI_SECRET": ""
    }
}
```

### Using Visual Studio
1) Open `text_summarization.sln` using Visual Studio 2022 or later.
2) Add this `local.settings.json` file to the `./text_summarization` folder to simplify local development.  Optionally fill in the AI_URL and AI_SECRET values per step 4 above.  This file will be gitignored to protect secrets from committing to your repo.  
```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "AI_URL": "",
        "AI_SECRET": ""
    }
}
```
3) Press Run (`F5`) to run in the debugger
4) Open Storage Explorer, Storage Accounts -> Emulator -> Blob Containers -> and create a container `test-samples-trigger` if it does not already exists
5) Copy any .txt document file with text into the `test-samples-trigger` container

You will see AI analysis happen in the Terminal standard out.  The analysis will be saved in a .txt file in the `` blob container.

### Using VS Code
1) Open the root folder in VS Code:

```bash
code .
```
2) Add this `local.settings.json` file to the `./text_summarization` folder to simplify local development.  Optionally fill in the AI_URL and AI_SECRET values per step 4 above.  This file will be gitignored to protect secrets from committing to your repo.  
```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "AI_URL": "",
        "AI_SECRET": ""
    }
}
```
3) Run and Debug by pressing `F5`
4) Open Storage Explorer, Storage Accounts -> Emulator -> Blob Containers -> and create a container `test-samples-trigger` if it does not already exists
5) Copy any .txt document file with text into the `test-samples-trigger` container

You will see AI analysis happen in the Terminal standard out.  The analysis will be saved in a .txt file in the `test-samples-output` blob container.

### Using Functions CLI
1) Open a new terminal and do the following:

```bash
cd text_summarization
dotnet build
func start --csharp
```
2) Open Storage Explorer, Storage Accounts -> Emulator -> Blob Containers -> and create a container `test-samples-trigger` if it does not already exists
3) Copy any .txt document file with text into the `test-samples-trigger` container

You will see AI analysis happen in the Terminal standard out.  The analysis will be saved in a .txt file in the `test-samples-output` blob container.

## Deploy to Azure

The easiest way to deploy this app is using the [Azure Dev CLI aka AZD](https://aka.ms/azd).  If you open this repo in GitHub CodeSpaces the AZD tooling is already preinstalled.

To provision and deploy:
1) Open a new terminal and do the following from root folder:
```bash
azd up
```
