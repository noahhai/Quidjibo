using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Quidjibo.Commands;
using Quidjibo.Handlers;
using Quidjibo.Misc;

namespace Quidjibo.EndToEnd
{
    public class Job
    {
        public class Command : IQuidjiboCommand
        {
            public int Id { get; }

            public Command(int id)
            {
                Id = id;
            }

            public Dictionary<string, string> Metadata { get; set; }
        }

        public class Handler : IQuidjiboHandler<Command>
        {
            public async Task ProcessAsync(Command command, IQuidjiboProgress progress, CancellationToken cancellationToken)
            {
                progress.Report(1, $"Starting item {command.Id}");
                await Task.Delay(25, cancellationToken);
                progress.Report(100, $"Finished item {command.Id}");
            }
        }
    }
}