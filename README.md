---
page_type: sample
languages:
- azdeveloper
- csharp
- bicep
products:
- azure
- azure-functions
- ai-services
- azure-cognitive-search
urlFragment: function-csharp-ai-textsummarize
name: Azure Functions - Text Summarization & Sentiment Analysis using AI Cognitive Language Service (C#-Isolated)
description: This sample shows how to take text documents as a input via BlobTrigger, does Text Summarization & Sentiment Score processing using the AI Congnitive Language service, and then outputs to another text document using BlobOutput binding. Deploys to Flex Consumption hosting plan of Azure Functions.
---
<!-- YAML front-matter schema: https://review.learn.microsoft.com/en-us/help/contribute/samples/process/onboarding?branch=main#supported-metadata-fields-for-readmemd -->

# Azure Functions
## Text Summarization & Sentiment Analysis using AI Cognitive Language Service (C#-Isolated)

This sample shows how to take text documents as a input via BlobTrigger, does Text Summarization processing using the [AI Congnitive Language Service](https://learn.microsoft.com/en-us/azure/ai-services/language-service/) ExtractiveSummarize operations, then computes sentiment scores, and then outputs to another text document using BlobOutput binding.  Deploys to Flex Consumption hosting plan of Azure Functions.

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/Azure-Samples/function-csharp-ai-textsummarize)

## Run on your local environment

### Pre-reqs
1) [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2) [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=v4%2Cmacos%2Ccsharp%2Cportal%2Cbash#install-the-azure-functions-core-tools)
3) [Azurite](https://github.com/Azure/Azurite)

The easiest way to install Azurite is using a Docker container or the support built into Visual Studio:
```bash
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

4) Once you have your Azure subscription, run the following in a new terminal window to create all the AI Language and other resources needed:
```bash
azd provision
```

Take note of the value of `TEXT_ANALYTICS_ENDPOINT` which can be found in `./.azure/<env name from azd provision>/.env`.  It will look something like:
```bash
TEXT_ANALYTICS_ENDPOINT="https://<unique string>.cognitiveservices.azure.com/"
```

Alternatively you can [create a Language resource](https://portal.azure.com/#create/Microsoft.CognitiveServicesTextAnalytics) in the Azure portal to get your key and endpoint. After it deploys, click Go to resource and view the Endpoint value.

5) [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/) or storage explorer features of [Azure Portal](https://portal.azure.com)
6) Add this `local.settings.json` file to the `./text_summarization` folder to simplify local development.  Optionally fill in the AI_URL and AI_SECRET values per step 4.  This file will be gitignored to protect secrets from committing to your repo.  
```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "TEXT_ANALYTICS_ENDPOINT": "<insert from step 4>"
    }
}
```

### Using Visual Studio
1) Open `text_summarization.sln` using Visual Studio 2022 or later.
2) Press Run (`F5`) to run in the debugger
3) Open Storage Explorer, Storage Accounts -> Emulator -> Blob Containers -> and create a container `unprocessed-text` if it does not already exists
4) Copy any .txt document file with text into the `unprocessed-text` container

You will see AI analysis happen in the Terminal standard out.  The analysis will be saved in a .txt file in the `processed-text` blob container.

### Using VS Code
1) Open the root folder in VS Code:

```bash
code .
```
2) Ensure `local.settings.json` exists already using steps above
3) Run and Debug by pressing `F5`
4) Open Storage Explorer, Storage Accounts -> Emulator -> Blob Containers -> and create a container `unprocessed-text` if it does not already exists
5) Copy any .txt document file with text into the `unprocessed-text` container
6) In the Azure extension of VS Code, open Azure:Workspace -> Local Project -> Functions -> `summarize_function`.  Right-click and Execute Function now.  At the command palette prompt, enter the path to the storage blob you just uploaded: `unprocessed-text/<your_text_filename.txt>`.  This will simulate an EventGrid trigger locally and your function will trigger and show output in the terminal.  

You will see AI analysis happen in the Terminal standard out.  The analysis will be saved in a .txt file in the `processed-text` blob container.

Note, this newer mechanism for BlobTrigger with EventGrid source is documented in more detail here: https://learn.microsoft.com/en-us/azure/azure-functions/functions-event-grid-blob-trigger?pivots=programming-language-python#run-the-function-locally. 

## Deploy to Azure

The easiest way to deploy this app is using the [Azure Developer CLI](https://aka.ms/azd).  If you open this repo in GitHub CodeSpaces the AZD tooling is already preinstalled.

To provision and deploy:
1) Open a new terminal and do the following from root folder:
```bash
azd up
```

## Understand the Code

The main operation of the code starts with the `summarize_function` function in [summarize_function.cs](./text_summarization/summarize_function.cs).  The function is triggered by a Blob uploaded event using BlobTrigger, your code runs to do the processing with AI, and then the output is returned as another blob file simply by returning a value and using the BlobOutput binding.  

```csharp
[Function("summarize_function")]
[BlobOutput("processed-text/{name}-output.txt")]
public async Task<string> Run(
    [BlobTrigger("unprocessed-text/{name}", Source = BlobTriggerSource.EventGrid)] string myTriggerItem,
    FunctionContext context)
{
    var logger = context.GetLogger("summarize_function");
    logger.LogInformation($"Triggered Item = {myTriggerItem}");

    // Create client using Entra User or Managed Identity (no longer AzureKeyCredential)
    // This requires a sub domain name to be set in endpoint URL for Managed Identity support
    // See https://learn.microsoft.com/en-us/azure/ai-services/authentication#authenticate-with-microsoft-entra-id 
    var client = new TextAnalyticsClient(endpoint, new DefaultAzureCredential());

    // analyze document text using Azure Cognitive Language Services
    var summarizedText = await AISummarizeText(client, myTriggerItem, logger);
    logger.LogInformation(Newline() + "*****Summary*****" + Newline() + summarizedText);

    // Blob Output
    return summarizedText;
}
```

The `AISummarizeText` helper function does the heavy lifting for summary extraction and sentiment analysis using the `TextAnalyticsClient` SDK from the [AI Language Services](https://learn.microsoft.com/en-us/azure/ai-services/language-service/):

```csharp
static async Task<string> AISummarizeText(TextAnalyticsClient client, string document, ILogger logger)
{
    // ...
    // Start analysis process.
    ExtractiveSummarizeOperation operation = client.ExtractiveSummarize(WaitUntil.Completed, batchInput);

    // View operation status.
    summarizedText += $"AnalyzeActions operation has completed" + Newline();
    summarizedText += $"Created On   : {operation.CreatedOn}" + Newline();
    summarizedText += $"Expires On   : {operation.ExpiresOn}" + Newline();
    summarizedText += $"Id           : {operation.Id}" + Newline();
    summarizedText += $"Status       : {operation.Status}" + Newline();

    // ...

    // Perform sentiment analysis on document summary
    var sentimentResult = await client.AnalyzeSentimentAsync(summarizedText);
    Console.WriteLine($"\nSentiment: {sentimentResult.Value.Sentiment}");
    Console.WriteLine($"Positive Score: {sentimentResult.Value.ConfidenceScores.Positive}");
    Console.WriteLine($"Negative Score: {sentimentResult.Value.ConfidenceScores.Negative}");
    Console.WriteLine($"Neutral Score: {sentimentResult.Value.ConfidenceScores.Neutral}");

    var summaryWithSentiment = summarizedText + $"Sentiment: {sentimentResult.Value.Sentiment}" + Newline();

    return summaryWithSentiment;
}

```
