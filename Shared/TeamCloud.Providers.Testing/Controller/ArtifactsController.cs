/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace TeamCloud.Providers.Testing.Services.Controller
{
    [ApiController]
    [Route("artifacts")]
    public sealed class ArtifactsController : ControllerBase
    {
        private const string DEFAULT_CONNECTION_STRING = "UseDevelopmentStorage=true";
        private const string DEPLOYMENT_CONTAINER_NAME = "deployments";

        private static readonly Lazy<CloudBlobContainer> deploymentContainer;

        static ArtifactsController()
        {
            deploymentContainer = new Lazy<CloudBlobContainer>(() => CloudStorageAccount
                .Parse(DEFAULT_CONNECTION_STRING)
                .CreateCloudBlobClient()
                .GetContainerReference(DEPLOYMENT_CONTAINER_NAME));
        }

        [HttpGet("{deploymentId:guid}/{artifactName}")]
        public async Task<IActionResult> Get(string deploymentId, string artifactName)
        {
            if (string.IsNullOrEmpty(deploymentId))
                throw new ArgumentException("message", nameof(deploymentId));

            if (string.IsNullOrEmpty(artifactName))
                throw new ArgumentException("message", nameof(artifactName));

            if (!deploymentContainer.IsValueCreated)
            {
                await deploymentContainer.Value
                    .CreateIfNotExistsAsync()
                    .ConfigureAwait(false);
            }

            var artifactBlob = deploymentContainer.Value
                .GetBlockBlobReference($"{deploymentId}/{artifactName}");

            var artifactExists = await artifactBlob
                .ExistsAsync()
                .ConfigureAwait(false);

            if (artifactExists)
            {
                var artifact = await artifactBlob
                    .DownloadTextAsync()
                    .ConfigureAwait(false);

                return new JsonResult(JObject.Parse(artifact));
            }

            return new NotFoundResult();
        }
    }
}
