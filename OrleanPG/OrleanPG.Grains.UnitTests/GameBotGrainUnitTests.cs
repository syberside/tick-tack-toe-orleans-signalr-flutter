using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using OrleanPG.Grains.GameBot;
using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Threading.Tasks;
using Xunit;

namespace OrleanPG.Grains.UnitTests
{
    public class GameBotGrainUnitTests
    {
        private readonly GameBotGrain _gameBot;
        private readonly Mock<GameBotGrain> _gameBotMock;
        private readonly Mock<IPersistentState<GameBotStorageData>> _storageMock;
        private readonly Mock<IGrainIdProvider> _idProviderMock;

        public GameBotGrainUnitTests()
        {
            _storageMock = new();
            _storageMock.SetupAllProperties();
            _idProviderMock = new();
            _gameBotMock = new(() => new GameBotGrain(_storageMock.Object, _idProviderMock.Object, new Random()));
            _gameBot = _gameBotMock.Object;
        }

        [Theory, AutoData]
        public async Task InitAsync_OnValidData_StoresTokenAndRole(AuthorizationToken token, bool playForX)
        {
            await _gameBot.InitAsync(token, playForX);

            _storageMock.Object.State.Should().Be(new GameBotStorageData(token, playForX));
            _storageMock.Verify(x => x.WriteStateAsync());
        }

        [Theory, AutoData]
        public async Task InitAsync_OnNullToken_Throws(bool playForX)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Func<Task> action = () => _gameBot.InitAsync(null, playForX);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Theory, AutoData]
        public async Task OnActivateAsync_Always_SubscribesForUpdates(Guid grainId)
        {
            var streamMock = SetupStreamMock(grainId);
            _gameBotMock.Setup(x => x.OnActivateAsync()).CallBase();

            await _gameBot.OnActivateAsync();

            streamMock.Verify(x => x.SubscribeAsync(It.IsAny<IAsyncObserver<GameStatusDto>>()));
        }

        [Theory]
        [InlineAutoData(GameState.TimedOut)]
        [InlineAutoData(GameState.XWin)]
        [InlineAutoData(GameState.OWin)]
        public async Task OnUpdateReceived_GameInEndStep_Cleanup(GameState gameState, GameStatusDto update, GameBotStorageData botState)
        {
            _storageMock.Object.State = botState;
            update = update with { Status = gameState };

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            await _gameBot.OnGameUpdated(update, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _storageMock.Verify(x => x.ClearStateAsync());
        }

        [Theory]
        [InlineAutoData(GameState.OTurn, false)]
        [InlineAutoData(GameState.XTurn, true)]
        public async Task OnUpdateReceived_BotTurn_PerformStep(GameState gameState, bool playForX, GameStatusDto update, GameBotStorageData botState, Guid gameId)
        {
            _idProviderMock.Setup(x => x.GetGrainId(_gameBot)).Returns(gameId);
            _storageMock.Object.State = botState with { PlayForX = playForX };
            update = update with { Status = gameState };
            var grainFactoryMock = new Mock<IGrainFactory>();
            var gameMock = new Mock<IGame>();
            grainFactoryMock.Setup(x => x.GetGrain<IGame>(gameId, null)).Returns(gameMock.Object);
            _gameBotMock.Setup(x => x.GrainFactory).Returns(grainFactoryMock.Object);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            await _gameBot.OnGameUpdated(update, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

#pragma warning disable CS8604 // Possible null reference argument.
            gameMock.Verify(x => x.TurnAsync(It.IsAny<int>(), It.IsAny<int>(), botState.Token));
#pragma warning restore CS8604 // Possible null reference argument.
        }



        private Mock<IAsyncStream<GameStatusDto>> SetupStreamMock(Guid grainId)
        {
            _idProviderMock.Setup(x => x.GetGrainId(_gameBot)).Returns(grainId);
            var streamMock = new Mock<IAsyncStream<GameStatusDto>>();
            var providerMock = new Mock<IStreamProvider>();
            providerMock.Setup(x => x.GetStream<GameStatusDto>(grainId, Constants.GameUpdatesStreamName)).Returns(streamMock.Object);
            _gameBotMock.Setup(x => x.GetStreamProvider(Constants.GameUpdatesStreamProviderName)).Returns(providerMock.Object);
            return streamMock;
        }
    }
}
