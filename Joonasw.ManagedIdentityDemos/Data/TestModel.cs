using System.ComponentModel.DataAnnotations.Schema;

namespace Joonasw.ManagedIdentityDemos.Data
{
    [Table("Test")]
    public class TestModel
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }
}