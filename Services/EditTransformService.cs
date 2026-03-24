using CollabDocs.Domain.Models;

namespace CollabDocs.Services;

public class EditTransformService
{
    public static ApplyEditOperationRequest Transform(
        ApplyEditOperationRequest incoming,
        IEnumerable<DocumentEdit> priorEdits)
    {
        var transformed = incoming;

        foreach (var prior in priorEdits)
        {
            string incomingType = transformed.OperationType?.Trim().ToLowerInvariant() ?? "";
            string priorType = prior.OperationType?.Trim().ToLowerInvariant() ?? "";

            if (incomingType == "insert")
            {
                if (priorType == "insert" && prior.Position <= transformed.Position)
                {
                    transformed = transformed with { Position = transformed.Position + (prior.Text?.Length ?? 0) };
                }
                else if (priorType == "delete" && prior.Position < transformed.Position)
                {
                    int deleteLen = prior.Length ?? 0;
                    if (prior.Position + deleteLen <= transformed.Position)
                        transformed = transformed with { Position = transformed.Position - deleteLen };
                    else
                        transformed = transformed with { Position = prior.Position };
                }
            }
            else if (incomingType == "delete" || incomingType == "replace")
            {
                if (priorType == "insert" && prior.Position <= transformed.Position)
                {
                    transformed = transformed with { Position = transformed.Position + (prior.Text?.Length ?? 0) };
                }
                else if (priorType == "delete" && prior.Position <= transformed.Position)
                {
                    int priorLen = prior.Length ?? 0;
                    if (prior.Position + priorLen <= transformed.Position)
                    {
                        transformed = transformed with { Position = transformed.Position - priorLen };
                    }
                    else
                    {
                        int overlap = (prior.Position + priorLen) - transformed.Position;
                        int newLength = (transformed.Length ?? 0) - overlap;
                        if (newLength < 0) newLength = 0;
                        transformed = transformed with { Position = prior.Position, Length = newLength };
                    }
                }
            }
        }

        return transformed;
    }
}