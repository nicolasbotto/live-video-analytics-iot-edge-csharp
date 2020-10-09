// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using httpExtension.Processors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace httpExtension.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Route("[controller]")]
    public class ScoreController : ControllerBase
    {
        private readonly ILogger<ScoreController> logger;

        public ScoreController(ILogger<ScoreController> logger)
        {
            this.logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage()
        {
            try
            {
                logger.LogInformation("Request received.");

                var stream = new MemoryStream();
                await Request.Body.CopyToAsync(stream);

                Image image = Image.FromStream(stream);

                var imageProcessor = new ImageProcessor(logger);

                var response = imageProcessor.ProcessImage(image);

                return new ObjectResult(response);
            }
            catch(Exception ex)
            {
                var errorMessage = $"An error occurred processing request: {ex.Message}";
                logger.LogError(errorMessage);
                return new BadRequestObjectResult(errorMessage);
            }
        }
    }
}
