using System.Collections.Generic;

namespace Joonasw.ManagedIdentityDemos.Models
{
    public class SqlDatabaseViewModel
    {
        public List<SqlRowModel> EfResults { get; set; }
        public List<SqlRowModel> AdoNetResults { get; set; }
    }
}
