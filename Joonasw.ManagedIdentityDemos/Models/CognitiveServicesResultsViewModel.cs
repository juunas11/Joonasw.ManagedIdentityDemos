using System.Collections.Generic;

namespace Joonasw.ManagedIdentityDemos.Models;

public class CognitiveServicesResultsViewModel
{
    public string Sentiment { get; set; }
    public Dictionary<string, double> ConfidenceScores { get; set; }
}
