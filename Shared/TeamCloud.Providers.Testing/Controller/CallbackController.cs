/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Testing.Services.Controller
{
    [ApiController]
    [Route("callback")]
    public sealed class CallbackController : ControllerBase
    {
        [HttpPost("{commandId:guid}")]
        public IActionResult Post(string commandId, [FromBody] ICommandResult commandResult)
        {
            if (!Guid.TryParse(commandId, out Guid commandIdParsed))
                return new NotFoundResult();

            if (commandResult is null)
                return new BadRequestResult();

            if (commandResult.CommandId != commandIdParsed)
                return new BadRequestResult();

            if (OrchestratorService.Instance.AddCommandResult(commandResult))
                return new OkResult();

            return new ConflictResult();
        }
    }
}
