using LosSantosExpanded.Systems;

namespace LosSantosExpanded
{
    public interface ISystem
    {
        void Initialize();

        void Update();

        void Shutdown();
    }
}