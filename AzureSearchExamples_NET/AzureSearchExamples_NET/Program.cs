using System;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Newtonsoft.Json;

namespace AzureSearchExamples_NET
{
    class Program
    {
        static void Main(string[] args)
        {
            //string serviceName = "<Put your search service NAME here>";
            string serviceName = "cognitive-search-east";
            //string apiKey = "<Put your search service ADMIN API KEY here>";
            string apiKey = "9F25B5D6440AED7ACEF4F8CC30C6A3F8";
            //string connectionString = "<Put your Azure SQL connection string here>";
            string connectionString = "Server=tcp:searchdemo.database.windows.net,1433;Initial Catalog=searchdemo;Persist Security Info=False;User ID=brianadmin;Password=D@rthVad3r0911;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            string indexName = "prod-index";

            // Create a SearchIndexClient to send create/delete index commands
            Uri serviceEndpoint = new Uri($"https://{serviceName}.search.windows.net/");
            AzureKeyCredential credential = new AzureKeyCredential(apiKey);
            SearchIndexClient adminClient = new SearchIndexClient(serviceEndpoint, credential);
            SearchIndexerClient indexerClient = new SearchIndexerClient(serviceEndpoint, credential);

            // Create a SearchClient to load and query documents
            SearchClient srchClient = new SearchClient(serviceEndpoint, indexName, credential);

            // Delete index if it exists
            Console.WriteLine("{0}", "Deleting index...\n");
            adminClient.GetIndexNames();
            {
                adminClient.DeleteIndex(indexName);
            }

            // Creating Data Source to Azure SQL with AdventureWorks DB
            Console.WriteLine("Creating data source...\n");
            var dataSource =
                new SearchIndexerDataSourceConnection(
                    "product-sql-ds",
                    SearchIndexerDataSourceType.AzureSql,
                    connectionString,
                    new SearchIndexerDataContainer("[SalesLT].[Product]"));

            // The data source does not need to be deleted if it was already created,
            // but the connection string may need to be updated if it was changed
            indexerClient.CreateOrUpdateDataSourceConnection(dataSource);

            // Create index
            Console.WriteLine("{0}", "Creating index...\n");
            FieldBuilder fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(Product));

            var prodIndex = new SearchIndex(indexName, searchFields);

            var suggester = new SearchSuggester("sg", new[] { "Name", "Color", "Size" });
            prodIndex.Suggesters.Add(suggester);

            adminClient.CreateOrUpdateIndex(prodIndex);

            // Create the Indexer to load documents from the Data Source created above
            Console.WriteLine("Creating Azure SQL indexer...\n");
            var schedule = new IndexingSchedule(TimeSpan.FromDays(1))
            {
                StartTime = DateTimeOffset.Now
            };

            var parameters = new IndexingParameters()
            {
                BatchSize = 100,
                MaxFailedItems = 0,
                MaxFailedItemsPerBatch = 0
            };

            // Indexer declarations require a data source and search index.
            // Common optional properties include a schedule, parameters, and field mappings
            // The field mappings below are redundant due to how the Product class is defined, but 
            // I included them anyway to show the syntax 
            var indexer = new SearchIndexer("product-sql-idxr", dataSource.Name, prodIndex.Name)
            {
                Description = "Data indexer",
                Schedule = schedule,
                Parameters = parameters,
                FieldMappings =
                {
                    new FieldMapping("ProductID") {TargetFieldName = "ProductID"},
                    new FieldMapping("Name") {TargetFieldName = "Name" },
                    new FieldMapping("ProductNumber") {TargetFieldName = "ProductNumber"},
                    new FieldMapping("ProductCategoryID") {TargetFieldName = "ProductCategoryID"},
                    new FieldMapping("Color") {TargetFieldName = "Color"},
                    new FieldMapping("Size") {TargetFieldName = "Size"},
                    new FieldMapping("ListPrice") {TargetFieldName = "ListPrice"},
                    new FieldMapping("DiscontinuedDate") {TargetFieldName = "DiscontinuedDate"}
                }
            };

            // Indexers contain metadata about how much they have already indexed
            // If we already ran the process, the indexer will remember that it already
            // indexed the product data and not run again
            // To avoid this, reset the indexer if it exists
            try
            {
                if (indexerClient.GetIndexer(indexer.Name) != null)
                {
                    indexerClient.ResetIndexer(indexer.Name);
                }
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                //if exception occurred and status is "Not Found", this is working as expected
                Console.WriteLine("Failed to find indexer and this is because it doesn't exist.\n");
            }
            indexerClient.CreateOrUpdateIndexer(indexer);

            // We created the indexer with a schedule, but we also
            // want to run it immediately
            Console.WriteLine("Running Azure SQL indexer...\n");
            indexerClient.RunIndexer(indexer.Name);
            
            // Wait 5 seconds for indexing to complete before checking status
            Console.WriteLine("Waiting for indexing...\n");
            System.Threading.Thread.Sleep(5000);

            // After an indexer run, you can retrieve status.
            CheckIndexerStatus(indexerClient, indexer);

            Console.WriteLine("Press any key to continue...\n");

            // Wait 2 seconds for indexing to complete before starting queries (for demo and console-app purposes only)
            Console.WriteLine("Waiting for indexing...\n");
            System.Threading.Thread.Sleep(2000);

            // Call the RunQueries method to invoke a series of queries
            Console.WriteLine("Starting queries...\n");
            RunQueries(srchClient);

            // End the program
            Console.WriteLine("{0}", "Complete. Press any key to end this program...\n");
            Console.ReadKey();

        }

        // Run queries, use WriteDocuments to print output
        private static void RunQueries(SearchClient srchclient)
        {
            SearchOptions options;
            SearchResults<Product> response;

            // Query 1
            Console.WriteLine("Query #1: Search on empty term '*' to return all documents, showing a subset of fields...\n");

            options = new SearchOptions()
            {
                IncludeTotalCount = true,
                Filter = "",
                OrderBy = { "" }
            };

            options.Select.Add("ProductID");
            options.Select.Add("Name");
            options.Select.Add("ProductNumber");
            options.Select.Add("Color");
            options.Select.Add("ListPrice");
            options.Select.Add("Size");
            options.Select.Add("DiscontinuedDate");

            response = srchclient.Search<Product>("*", options);
            WriteDocuments(response);

            // Query 2
            Console.WriteLine("Query #2: Search on empty term '*' to return all documents, but filter on 'Color eq Black', sort by ListPrice in descending order...\n");

            options = new SearchOptions()
            {
                Filter = "Color eq 'Black'",
                OrderBy = { "ListPrice desc" }
            };

            options.Select.Add("ProductID");
            options.Select.Add("Name");
            options.Select.Add("ProductNumber");
            options.Select.Add("Color");
            options.Select.Add("ListPrice");
            options.Select.Add("Size");
            options.Select.Add("DiscontinuedDate");

            response = srchclient.Search<Product>("*", options);
            WriteDocuments(response);

            // Query 3
            Console.WriteLine("Query #3: Limit search to specific fields (Frame in Name field)...\n");

            options = new SearchOptions()
            {
                SearchFields = { "Name" }
            };

            options.Select.Add("ProductID");
            options.Select.Add("Name");
            options.Select.Add("ProductNumber");
            options.Select.Add("Color");
            options.Select.Add("ListPrice");
            options.Select.Add("Size");
            options.Select.Add("DiscontinuedDate");

            response = srchclient.Search<Product>("Frame", options);
            WriteDocuments(response);

            // Query 4
            Console.WriteLine("Query #4: Look up a specific document...\n");

            Response<Product> lookupResponse;
            lookupResponse = srchclient.GetDocument<Product>("706");

            Console.WriteLine(JsonConvert.SerializeObject(lookupResponse.Value));

            // Query 5
            Console.WriteLine("Query #5: Call Autocomplete on Name...\n");

            var autoresponse = srchclient.Autocomplete("HL", "sg");
            WriteDocuments(autoresponse);

        }

        // Write search results to console
        private static void WriteDocuments(SearchResults<Product> searchResults)
        {
            foreach (SearchResult<Product> result in searchResults.GetResults())
            {
                Console.WriteLine(JsonConvert.SerializeObject(result.Document));
            }

            Console.WriteLine();
        }

        private static void WriteDocuments(AutocompleteResults autoResults)
        {
            foreach (AutocompleteItem result in autoResults.Results)
            {
                Console.WriteLine(result.Text);
            }

            Console.WriteLine();
        }

        private static void CheckIndexerStatus(SearchIndexerClient indexerClient, SearchIndexer indexer)
        {
            try
            {
                SearchIndexerStatus execInfo = indexerClient.GetIndexerStatus(indexer.Name);

                Console.WriteLine("Indexer has run {0} times.", execInfo.ExecutionHistory.Count);
                Console.WriteLine("Indexer Status: " + execInfo.Status.ToString());

                IndexerExecutionResult result = execInfo.LastResult;

                Console.WriteLine("Latest run");
                Console.WriteLine("Run Status: {0}", result.Status.ToString());
                Console.WriteLine("Total Documents: {0}, Failed: {1}", result.ItemCount, result.FailedItemCount);

                TimeSpan elapsed = result.EndTime.Value - result.StartTime.Value;
                Console.WriteLine("StartTime: {0:T}, EndTime: {1:T}, Elapsed: {2:t}", result.StartTime.Value, result.EndTime.Value, elapsed);

                string errorMsg = (result.ErrorMessage == null) ? "none" : result.ErrorMessage;
                Console.WriteLine("ErrorMessage: {0}", errorMsg);
                Console.WriteLine(" Document Errors: {0}, Warnings: {1}\n", result.Errors.Count, result.Warnings.Count);
            }
            catch (Exception e)
            {
                Console.WriteLine("Indexer Status Error: ", e.Message);
            }
        }
    }
}
