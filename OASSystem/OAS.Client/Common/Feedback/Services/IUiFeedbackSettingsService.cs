using OAS.Client.Common.Feedback.Models;

namespace OAS.Client.Common.Feedback.Services;

public interface IUiFeedbackSettingsService
{
    UiFeedbackSettings Current { get; }
    void Apply(UiFeedbackSettings settings);
}
