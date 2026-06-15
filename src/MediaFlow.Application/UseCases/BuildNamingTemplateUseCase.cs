using System.Text;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Application.UseCases;

public sealed class BuildNamingTemplateUseCase(IEnumerable<INamingTokenResolver> resolvers)
{
    public string Execute(
        IReadOnlyList<NamingToken> template,
        string originalFileName,
        string? exifCaptureDate,
        int sequenceNumber)
    {
        if (template.Count == 0)
            return originalFileName;

        var extension = Path.GetExtension(originalFileName);
        var sb = new StringBuilder();

        foreach (var token in template)
        {
            var resolver = resolvers.FirstOrDefault(r => r.CanResolve(token));
            if (resolver is not null)
                sb.Append(resolver.Resolve(token, exifCaptureDate, sequenceNumber));
        }

        return sb.ToString() + extension;
    }
}
