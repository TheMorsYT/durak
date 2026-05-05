using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.Events;

namespace Durak.Architecture.Shared.Interfaces
{
    public interface IMatchController
    {
        MatchContext Context { get; }
        IMatchEventBus EventBus { get; }

        void Initialize();
        void Tick(float deltaTime);
    }
}
