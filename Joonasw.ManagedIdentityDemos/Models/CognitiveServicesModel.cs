using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Joonasw.ManagedIdentityDemos.Models;

public class CognitiveServicesModel
{
    [Required, MinLength(1), MaxLength(100)]
    public string Input { get; set; }

    public string Sentiment { get; set; }
    public Dictionary<string, double> ConfidenceScores { get; set; }
}
