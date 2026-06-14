using MediaFlow.Domain.Entities;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Domain.Interfaces;

public interface INamingTokenResolver
{
    bool CanResolve(NamingToken token);
    string Resolve(NamingToken token, FileContext context, int sequenceNumber);
}
