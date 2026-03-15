using System;
using System.Collections.Generic;
using System.Text;

namespace AiMultiAgent.Application.Interfaces
{
    public interface IOrchestratorAgent
    {
        Task<string> ProcessAsync(string prompt);
    }
}
