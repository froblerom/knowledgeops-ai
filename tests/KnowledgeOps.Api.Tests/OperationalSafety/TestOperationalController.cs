using KnowledgeOps.Application.Errors;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Api.Tests.OperationalSafety;

[ApiController]
[Route("api/test/operational")]
public sealed class TestOperationalController : ControllerBase
{
    [HttpGet("validation")]
    public IActionResult Validation() =>
        throw new ApplicationValidationException(
            [new ApplicationValidationItem("name", "Name is required.")]);

    [HttpGet("unauthenticated")]
    public IActionResult Unauthenticated() =>
        throw new ApplicationUnauthenticatedException();

    [HttpGet("forbidden")]
    public IActionResult ForbiddenFailure() =>
        throw new ApplicationForbiddenException();

    [HttpGet("not-found")]
    public IActionResult NotFoundFailure() =>
        throw new ApplicationNotFoundException();

    [HttpGet("conflict")]
    public IActionResult ConflictFailure() =>
        throw new ApplicationConflictException();

    [HttpGet("unavailable")]
    public IActionResult Unavailable() =>
        throw new ApplicationServiceUnavailableException();

    [HttpGet("unexpected")]
    public IActionResult Unexpected() =>
        throw new InvalidOperationException("a connection string or secret must not reach the response");
}
