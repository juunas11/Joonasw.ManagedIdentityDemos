using Joonasw.ManagedIdentityDemos.Models.AzureMaps;
using System.ComponentModel.DataAnnotations;

namespace Joonasw.ManagedIdentityDemos.Models;

public class AzureMapsViewModel
{
    public MapsPoiResults Results { get; set; }

    [Required, MinLength(1), MaxLength(100)]
    public string Input { get; set; }
}
