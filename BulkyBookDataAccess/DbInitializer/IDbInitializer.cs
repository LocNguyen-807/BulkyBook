using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookDataAccess.DbInitializer
{
    public interface IDbInitializer
    {
        Task InitializeAsync();
    }
}
