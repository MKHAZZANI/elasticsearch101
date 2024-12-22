using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MyEcommerceAPI.Models;
using MyEcommerceAPI.MongoDbSettings;
using Nest;
using MyEcommerceAPI.ElasticsearchSettings;

namespace MyEcommerceAPI.Services
{
    public class ProductService
    {
        private readonly IMongoCollection<Product> _productsCollection;
        private readonly IElasticClient _elasticClient;
        private readonly string _productIndexName;

        public ProductService(IOptions<MongoDbConfig> mongoConfig, IOptions<ElasticSettings> elasticConfig, IMongoClient mongoClient, IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
            _productIndexName = elasticConfig.Value.ProductIndexName;

            var database = mongoClient.GetDatabase(mongoConfig.Value.DatabaseName);
            _productsCollection = database.GetCollection<Product>(mongoConfig.Value.ProductsCollectionName);
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _productsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(string id)
        {
            return await _productsCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(Product newProduct)
        {
            await _productsCollection.InsertOneAsync(newProduct);
            await IndexProductAsync(newProduct);
        }

        public async Task UpdateAsync(string id, Product updatedProduct)
        {
            await _productsCollection.ReplaceOneAsync(p => p.Id == id, updatedProduct);
        }

        public async Task DeleteAsync(string id)
        {
            await _productsCollection.DeleteOneAsync(p => p.Id == id);
        }

        public async Task IndexProductAsync(Product product)
        {
            var response = await _elasticClient.IndexAsync(product, idx => idx
                .Index(_productIndexName)
                .Id(product.Id)
            );

            if (!response.IsValid)
            {
                
            }
        }

        public async Task<List<Product>> SearchAsync(string query, string? category = null)
        {
            var searchRequest = new SearchRequest(_productIndexName)
            {
                Query = new BoolQuery
                {
                    Must = new List<QueryContainer>
                    {
                        new MultiMatchQuery
                        {
                            Query = query,
                            Fields = new[] { "title^2", "description" },
                            Fuzziness = Fuzziness.Auto,
                            Operator = Operator.Or
                        }
                    },
                    Filter = category != null ? new List<QueryContainer>
                    {
                        new TermQuery
                        {
                            Field = "category.keyword",
                            Value = category
                        }
                    } : null
                }
            };

            var response = await _elasticClient.SearchAsync<Product>(searchRequest);

            if (!response.IsValid)
            {
                throw new Exception($"Elasticsearch error: {response.DebugInformation}");
            }

            return response.Documents.ToList();
        }

        public async Task CreateIndexIfNotExists()
        {
            var indexExists = await _elasticClient.Indices.ExistsAsync(_productIndexName);
            if (!indexExists.Exists)
            {
                var createIndexResponse = await _elasticClient.Indices.CreateAsync(_productIndexName, c => c
                    .Map<Product>(m => m
                        .Properties(p => p
                            .Text(t => t.Name(n => n.Title))
                            .Text(t => t.Name(n => n.Description))
                            .Keyword(k => k.Name(n => n.Category))
                            .Number(n => n.Name(x => x.Price))
                        )
                    )
                );
            }
        }

        public async Task IndexAllProducts()
        {
            var products = await GetAllAsync();
            var bulkDescriptor = new BulkDescriptor();
            
            foreach (var product in products)
            {
                bulkDescriptor.Index<Product>(i => i
                    .Index(_productIndexName)
                    .Id(product.Id)
                    .Document(product)
                );
            }
            
            await _elasticClient.BulkAsync(bulkDescriptor);
        }

        /*public async Task<List<Product>> AutocompleteAsync(string query)
        {
        
            var response = await _elasticClient.SearchAsync<Product>(s => s
                .Index(_productIndexName)
                .Query(q => q
                    .Prefix(p => p
                        .Field(f => f.Title)
                        .Value(query.ToLower())
                    )
                )
                .Size(5)
            );

            if (!response.IsValid)
            {
                return new List<Product>();
            }

            return response.Documents.ToList();
        }*/

//********************Simple Way **********************
        /*public async Task<List<Product>> AutocompleteAsync(string query)
        {
            var response = await _elasticClient.SearchAsync<Product>(s => s
                .Index(_productIndexName)
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Query(query)
                        .Fields(f => f
                            .Field(ff => ff.Title)
                            .Field(ff => ff.Description)
                        )
                        .Type(TextQueryType.PhrasePrefix) 
                    )
                )
                .Size(5)
            );

            if (!response.IsValid)
            {
                return new List<Product>();
            }

            return response.Documents.ToList();
        }

//**************Boolean should Query Approach*******************
        */
        public async Task<List<Product>> AutocompleteAsync(string query)
        {
            var response = await _elasticClient.SearchAsync<Product>(s => s
                .Index(_productIndexName)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            sh => sh.Prefix(p => p
                                .Field(f => f.Title)
                                .Value(query.ToLower())
                            ),
                            sh => sh.Prefix(p => p
                                .Field(f => f.Description)
                                .Value(query.ToLower())
                            )
                        )
                        .MinimumShouldMatch(1)
                    )
                )
                .Size(5)
            );

            if (!response.IsValid)
            {
                return new List<Product>();
            }

            return response.Documents.ToList();
        }
        
    }
}