using System;
using System.Collections.Generic;
using System.Text;

namespace AiMultiAgent.Infrastructure.Rabbit.Infaces;

public interface IRabbitPublisher
{
    Task PublishAsync(string message);
}
