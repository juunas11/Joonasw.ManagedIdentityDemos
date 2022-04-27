using System.ComponentModel.DataAnnotations;

namespace Joonasw.ManagedIdentityDemos.Models;

public class CognitiveServicesInputModel
{
    [Required, MinLength(1), MaxLength(100)]
    public string Input { get; set; }
}
