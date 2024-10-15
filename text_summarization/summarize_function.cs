using Azure;
using Azure.AI.TextAnalytics;
using Azure.Core;
using Azure.Core.Diagnostics;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AI_Functions
{
    public class summarize_function
    {
        private readonly ILogger _logger;

        private TokenCredential credential;

        private Uri endpoint;

        public summarize_function(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<summarize_function>();

            var endpointUri = Environment.GetEnvironmentVariable("TEXT_ANALYTICS_ENDPOINT") 
                              ?? throw new InvalidOperationException("Environment variable 'TEXT_ANALYTICS_ENDPOINT' is not set.");
            endpoint = new Uri(endpointUri);

            credential = new DefaultAzureCredential();
        }

        [Function("summarize_function")]
        [BlobOutput("processed-text/{name}-output.txt")]
        public async Task<string> Run(
            [BlobTrigger("unprocessed-text/{name}", Source = BlobTriggerSource.EventGrid )] string myTriggerItem,
            FunctionContext context)
        {
            var logger = context.GetLogger("summarize_function");
            logger.LogInformation($"Triggered Item = {myTriggerItem}");

            // Create client using Entra User or Managed Identity (no longer AzureKeyCredential)
            // This requires a sub domain name to be set in endpoint URL for Managed Identity support
            // See https://learn.microsoft.com/en-us/azure/ai-services/authentication#authenticate-with-microsoft-entra-id 
            var client = new TextAnalyticsClient(endpoint, credential);

            // analyze document text using Azure Cognitive Language Services
            var summarizedText = await AISummarizeText(client, myTriggerItem, logger);
            logger.LogInformation(Newline() + "*****Summary*****" + Newline() + summarizedText);

            // Blob Output
            return summarizedText;
        }
        private async Task<string> AISummarizeText(TextAnalyticsClient client, string document, ILogger logger)
        {

            // Perform Extractive Summarization
            string summarizedText = "";

            // Prepare analyze operation input. You can add multiple documents to this list and perform the same
            // operation to all of them.
            var batchInput = new List<string>
            {
                document
            };

            TextAnalyticsActions actions = new TextAnalyticsActions()
            {
                ExtractiveSummarizeActions = new List<ExtractiveSummarizeAction>() { new ExtractiveSummarizeAction() }
            };

            // Start analysis process.
            ExtractiveSummarizeOperation operation = client.ExtractiveSummarize(WaitUntil.Completed, batchInput);
        
            // View operation status.
            summarizedText += $"AnalyzeActions operation has completed" + Newline();
            summarizedText += $"Created On   : {operation.CreatedOn}" + Newline();
            summarizedText += $"Expires On   : {operation.ExpiresOn}" + Newline();
            summarizedText += $"Id           : {operation.Id}" + Newline();
            summarizedText += $"Status       : {operation.Status}" + Newline();

            // View operation results.
            await foreach (ExtractiveSummarizeResultCollection documentsInPage in operation.Value)
            {
                Console.WriteLine($"Extractive Summarize, version: \"{documentsInPage.ModelVersion}\"");
                Console.WriteLine();

                foreach (ExtractiveSummarizeResult documentResult in documentsInPage)
                {
                    if (documentResult.HasError)
                    {
                        Console.WriteLine($"  Error!");
                        Console.WriteLine($"  Document error code: {documentResult.Error.ErrorCode}");
                        Console.WriteLine($"  Message: {documentResult.Error.Message}");
                        continue;
                    }

                    Console.WriteLine($"  Extracted {documentResult.Sentences.Count} sentence(s):");
                    Console.WriteLine();

                    foreach (ExtractiveSummarySentence sentence in documentResult.Sentences)
                    {
                        Console.WriteLine($"  Sentence: {sentence.Text}");
                        Console.WriteLine($"  Rank Score: {sentence.RankScore}");
                        Console.WriteLine($"  Offset: {sentence.Offset}");
                        Console.WriteLine($"  Length: {sentence.Length}");
                        Console.WriteLine();

                        summarizedText += $"  Sentence: {sentence.Text}" + Newline();
                    }
                }

                
            }

            logger.LogInformation(Newline() + "*****Summary*****" + Newline() + summarizedText);

            // Perform sentiment analysis on document summary
            var sentimentResult = await client.AnalyzeSentimentAsync(summarizedText);
            Console.WriteLine($"\nSentiment: {sentimentResult.Value.Sentiment}");
            Console.WriteLine($"Positive Score: {sentimentResult.Value.ConfidenceScores.Positive}");
            Console.WriteLine($"Negative Score: {sentimentResult.Value.ConfidenceScores.Negative}");
            Console.WriteLine($"Neutral Score: {sentimentResult.Value.ConfidenceScores.Neutral}");
    
            var summaryWithSentiment = summarizedText + $"Sentiment: {sentimentResult.Value.Sentiment}" + Newline();
            
            return summaryWithSentiment;
        }

        private string Newline()
        {
            return "\r\n";
        }

    }


}
