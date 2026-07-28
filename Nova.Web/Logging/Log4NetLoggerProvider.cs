using log4net;
using log4net.Repository;
using Microsoft.Extensions.Logging;

namespace Nova.Web.Logging
{
    public sealed class Log4NetLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerRepository _repository;

        public Log4NetLoggerProvider(ILoggerRepository repository)
        {
            _repository = repository;
        }

        public ILogger CreateLogger(string categoryName)
            => new Log4NetLogger(LogManager.GetLogger(_repository.Name, categoryName));

        public void Dispose()
        {
        }
    }
}
