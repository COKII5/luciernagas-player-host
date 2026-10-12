using Fireflies.Session;

namespace Fireflies.App.Input
{
    public interface IInputSource
    {
        LocalInput ConsumeFixedStepInput();
    }
}
