using OAS.Contracts.Common.Errors;

namespace OAS.Client.Common.Feedback.Services;

public interface IApiFeedbackService
{
    void Show(ApiError error);
    void ShowUnexpected();
}
