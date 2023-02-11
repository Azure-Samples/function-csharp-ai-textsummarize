using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AI_Functions
{
    public class summarize_function
    {
        private readonly ILogger _logger;

        // must export and set these Env vars with your AI Cognitive Language resource values
        private static readonly AzureKeyCredential credentials = new AzureKeyCredential(Environment.GetEnvironmentVariable("AI_SECRET") ?? "SETENVVAR!");
        private static readonly Uri endpoint = new Uri(Environment.GetEnvironmentVariable("AI_URL") ?? "SETENVVAR!");

        public summarize_function(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<summarize_function>();
        }

        [Function("summarize_function")]
        [BlobOutput("test-samples-output/{name}-output.txt")]
        public static async Task<string> Run(
            [BlobTrigger("test-samples-trigger/{name}")] string myTriggerItem,
            FunctionContext context)
        {
            var logger = context.GetLogger("summarize_function");
            logger.LogInformation($"Triggered Item = {myTriggerItem}");

            var client = new TextAnalyticsClient(endpoint, credentials);

            // analyze document text using Azure Cognitive Language Services
            var summarizedText = await AISummarizeText(client, myTriggerItem, logger);
            logger.LogInformation(Newline() + "*****Summary*****" + Newline() + summarizedText);

            // Blob Output
            return summarizedText;
        }
        static async Task<string> AISummarizeText(TextAnalyticsClient client, string document, ILogger logger)
        {

            string summarizedText = "";

            // Prepare analyze operation input. You can add multiple documents to this list and perform the same
            // operation to all of them.
            var batchInput = new List<string>
            {
                document
            };

            TextAnalyticsActions actions = new TextAnalyticsActions()
            {
                ExtractSummaryActions = new List<ExtractSummaryAction>() { new ExtractSummaryAction() }
            };

            // Start analysis process.
            AnalyzeActionsOperation operation = await client.StartAnalyzeActionsAsync(batchInput, actions);
            await operation.WaitForCompletionAsync();
            // View operation status.
            summarizedText += $"AnalyzeActions operation has completed" + Newline();
            summarizedText += $"Created On   : {operation.CreatedOn}" + Newline();
            summarizedText += $"Expires On   : {operation.ExpiresOn}" + Newline();
            summarizedText += $"Id           : {operation.Id}" + Newline();
            summarizedText += $"Status       : {operation.Status}" + Newline();

            // View operation results.
            await foreach (AnalyzeActionsResult documentsInPage in operation.Value)
            {
                IReadOnlyCollection<ExtractSummaryActionResult> summaryResults = documentsInPage.ExtractSummaryResults;

                foreach (ExtractSummaryActionResult summaryActionResults in summaryResults)
                {
                    if (summaryActionResults.HasError)
                    {
                        logger.LogError($"  Error!");
                        logger.LogError($"  Action error code: {summaryActionResults.Error.ErrorCode}.");
                        logger.LogError($"  Message: {summaryActionResults.Error.Message}");
                        continue;
                    }

                    foreach (ExtractSummaryResult documentResults in summaryActionResults.DocumentsResults)
                    {
                        if (documentResults.HasError)
                        {
                            logger.LogError($"  Error!");
                            logger.LogError($"  Document error code: {documentResults.Error.ErrorCode}.");
                            logger.LogError($"  Message: {documentResults.Error.Message}");
                            continue;
                        }

                        summarizedText += $"  Extracted the following {documentResults.Sentences.Count} sentence(s):" + Newline();


                        foreach (SummarySentence sentence in documentResults.Sentences)
                        {
                            summarizedText += $"  Sentence: {sentence.Text}" + Newline();
                        }
                    }
                }
            }

            logger.LogInformation(Newline() + "*****Summary*****" + Newline() + summarizedText);
            return summarizedText;
        }

        static string Newline()
        {
            return "\r\n";
        }

    }


}
