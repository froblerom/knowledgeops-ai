using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace KnowledgeOps.Api.Controllers.Models;

public sealed class DocumentUploadRequest
{
    [Required]
    [MaxLength(300)]
    public string? Title { get; init; }

    [Required]
    public IFormFile? File { get; init; }
}
