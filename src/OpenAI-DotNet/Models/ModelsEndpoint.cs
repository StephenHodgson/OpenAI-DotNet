﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAI.Models
{
    /// <summary>
    /// List and describe the various models available in the API.
    /// You can refer to the Models documentation to understand what <see href="https://beta.openai.com/docs/models"/> are available and the differences between them.
    /// <see href="https://beta.openai.com/docs/api-reference/models"/>
    /// </summary>
    public sealed class ModelsEndpoint : BaseEndPoint
    {
        private sealed class ModelsList
        {
            [JsonPropertyName("data")]
            public List<Model> Data { get; set; }
        }

        private sealed class DeleteModelResponse
        {
            [JsonConstructor]
            public DeleteModelResponse(string id, string @object, bool deleted)
            {
                Id = id;
                Object = @object;
                Deleted = deleted;
            }

            [JsonPropertyName("id")]
            public string Id { get; }

            [JsonPropertyName("object")]
            public string Object { get; }

            [JsonPropertyName("deleted")]
            public bool Deleted { get; }
        }

        /// <inheritdoc />
        public ModelsEndpoint(OpenAIClient api) : base(api) { }

        /// <inheritdoc />
        protected override string Root => "models";

        /// <summary>
        /// List all models via the API
        /// </summary>
        /// <returns>Asynchronously returns the list of all <see cref="Model"/>s</returns>
        /// <exception cref="HttpRequestException">Raised when the HTTP request fails</exception>
        public async Task<IReadOnlyList<Model>> GetModelsAsync()
        {
            var response = await Api.Client.GetAsync(GetUrl()).ConfigureAwait(false);
            var responseAsString = await response.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<ModelsList>(responseAsString, Api.JsonSerializationOptions)?.Data;
        }

        /// <summary>
        /// Get the details about a particular Model from the API
        /// </summary>
        /// <param name="id">The id/name of the model to get more details about</param>
        /// <returns>Asynchronously returns the <see cref="Model"/> with all available properties</returns>
        /// <exception cref="HttpRequestException">Raised when the HTTP request fails</exception>
        public async Task<Model> GetModelDetailsAsync(string id)
        {
            var response = await Api.Client.GetAsync(GetUrl($"/{id}")).ConfigureAwait(false);
            var responseAsString = await response.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<Model>(responseAsString, Api.JsonSerializationOptions);
        }

        /// <summary>
        /// Delete a fine-tuned model. You must have the Owner role in your organization.
        /// </summary>
        /// <param name="modelId">The <see cref="Model"/> to delete.</param>
        /// <returns>True, if fine-tuned model was successfully deleted.</returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task<bool> DeleteFineTuneModelAsync(string modelId)
        {
            var model = await GetModelDetailsAsync(modelId).ConfigureAwait(false);

            if (model == null)
            {
                throw new Exception($"Failed to get {modelId} info!");
            }

            try
            {
                var response = await Api.Client.DeleteAsync(GetUrl($"/{model.Id}")).ConfigureAwait(false);
                var responseAsString = await response.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<DeleteModelResponse>(responseAsString, Api.JsonSerializationOptions)?.Deleted ?? false;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("api.delete"))
                {
                    throw new UnauthorizedAccessException($"You do not have permissions to delete models for this organization.\n{e}");
                }

                throw;
            }
        }
    }
}
