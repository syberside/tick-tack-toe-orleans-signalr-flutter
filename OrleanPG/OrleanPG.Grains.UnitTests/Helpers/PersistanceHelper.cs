using Moq;
using Orleans.Runtime;
using System.Threading.Tasks;

namespace OrleanPG.Grains.GameLobbyGrain.UnitTests.Helpers
{
    public static class PersistanceHelper
    {
        public static Mock<IPersistentState<T>> CreateAndSetupStateWriteMock<T>() where T : new()
        {
            var result = new Mock<IPersistentState<T>>();
            result.SetupProperty(x => x.State);
            result.Object.State = new T();
            result.Setup(x => x.WriteStateAsync()).Returns(Task.CompletedTask);
            return result;
        }

    }
}
