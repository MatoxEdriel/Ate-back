using Ate.Application.Common.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("files")]
[AllowAnonymous]
[Tags("Files")]
public class FilesController : ControllerBase
{
    private const string ImmutableCache = "public, max-age=31536000, immutable";

    private readonly IFileStorage _storage;

    public FilesController(IFileStorage storage)
    {
        _storage = storage;
    }

    [HttpGet("{**key}")]
    [EndpointSummary("Get a public file (logos)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        if (!StorageKeys.IsValid(key))
        {
            return NotFound();
        }

        var file = await _storage.OpenReadAsync(FileArea.Public, key, cancellationToken);
        if (file == null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = ImmutableCache;
        return File(file.Content, file.ContentType);
    }
}
