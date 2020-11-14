using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Odyssey.API.Infrastructure.ActionResults
{
    public class InternalServerErrorObjectResult : ObjectResult
    {
        public InternalServerErrorObjectResult(object error)
            : base(error)
        {
            StatusCode = StatusCodes.Status500InternalServerError;
        }

    }
}
