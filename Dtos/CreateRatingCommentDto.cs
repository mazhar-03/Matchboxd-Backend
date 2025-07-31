using System.ComponentModel.DataAnnotations;

namespace Matchboxd.API.Dtos;

public class CreateRatingCommentDto
{
    [Range(0.5, 5.0)] public double? Score { get; set; } // Optional

    public string? Content { get; set; } // Optional
}

