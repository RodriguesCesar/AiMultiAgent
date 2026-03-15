using System;
using System.Collections.Generic;
using System.Text;

namespace AiMultiAgent.Infrastructure.Data.MongoDB
{
    public interface IMongoRepository
    {
        Task SaveAsync(string message, string response);
    }
}
