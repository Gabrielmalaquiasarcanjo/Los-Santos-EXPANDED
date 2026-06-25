using LosSantosExpanded.lse.Systems.Core;

namespace LosSantosExpanded.lse.Systems.Core
{
    public interface ISystem
    {
        void Initialize();

        void Update();

        void Shutdown();
    }
}