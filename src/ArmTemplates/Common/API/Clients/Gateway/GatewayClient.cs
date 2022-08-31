﻿// --------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License.
// --------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Constants;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Gateway;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Gateway
{
    public class GatewayClient : ApiClientBase, IGatewayClient
    {
        const string GetAllGatewaysRequest = "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/gateways?api-version={4}";

        readonly ILogger<GatewayClient> logger;
        readonly IApisClient apisClient;
        readonly ITemplateResourceDataProcessor<GatewayTemplateResource> templateResourceDataProcessor;

        public GatewayClient(
            IHttpClientFactory httpClientFactory,
            ILogger<GatewayClient> logger,
            IApisClient apisClient,
            ITemplateResourceDataProcessor<GatewayTemplateResource> templateResourceDataProcessor
            ) : base(httpClientFactory)
        {
            this.logger = logger;
            this.apisClient = apisClient;
            this.templateResourceDataProcessor = templateResourceDataProcessor;
        }

        public async Task<List<GatewayTemplateResource>> GetAllAsync(ExtractorParameters extractorParameters)
        {
            var (azToken, azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format(GetAllGatewaysRequest,
               this.BaseUrl, azSubId, extractorParameters.ResourceGroup, extractorParameters.SourceApimName, GlobalConstants.ApiVersion);

            var gatewatTemplateResources = await this.GetPagedResponseAsync<GatewayTemplateResource>(azToken, requestUrl);
            this.templateResourceDataProcessor.ProcessData(gatewatTemplateResources);
            return gatewatTemplateResources;
        }

        /// <summary>
        /// Checks whether a given single API is referenced by a gateway
        /// </summary>
        /// <returns>true, if api references a gateway</returns>
        public async Task<bool> DoesApiReferenceGatewayAsync(string singleApiName, string gatewayName, ExtractorParameters extractorParameters)
        {
            var gatewayApis = await this.apisClient.GetAllLinkedToGatewayAsync(gatewayName, extractorParameters);

            if (gatewayApis.IsNullOrEmpty())
            {
                this.logger.LogDebug("Did not find any api linked to the gateway");
                return false;
            }

            return gatewayApis.Any(gatewayApi => gatewayApi.Name == singleApiName);
        }
    }
}
