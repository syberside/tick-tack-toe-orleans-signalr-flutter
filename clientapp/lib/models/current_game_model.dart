import 'package:clientapp/pages/lobbies_page.dart';
import 'package:flutter/foundation.dart';

class CurrentGameModel extends ChangeNotifier {
  UserGameParticipation _participation = UserGameParticipation.readOnly;
  GameStatus? _status;
  List<List<CellStatus>> _gameMap = GameData.createEmptyMap();
  GameGeneralInfo? _generalInfo;

  CurrentGameModel(Stream<GameStatusDto> gameUpdates) {
    gameUpdates.listen((event) {
      _gameMap = event.gameMap;
      _status = event.status;
      //TODO: this is hack. info should be provided by event
      if (_participation == UserGameParticipation.playForX) {
        _generalInfo!.playerO = "bla";
      }
      if (_participation == UserGameParticipation.playForO) {
        _generalInfo!.playerX = "bla";
      }
      print("Update processed: $event");
      notifyListeners();
    });
  }

  UserGameParticipation get participation => _participation;
  GameStatus? get status => _status;
  List<List<CellStatus>> get gameMap => _gameMap;
  GameGeneralInfo? get generalInfo => _generalInfo;

  void makeOptimisticTurn(int i, int j) {
    _gameMap[i][j] = participation == UserGameParticipation.playForX
        ? CellStatus.X
        : CellStatus.O;
    _status = status == GameStatus.XTurn ? GameStatus.OTurn : GameStatus.XTurn;
    notifyListeners();
  }

  void newGameCreated(GameData gameData, bool playForX) {
    //TODO: remove gamedata?
    _gameMap = gameData.gameMap;
    _generalInfo = gameData.generalInfo;
    _participation = playForX
        ? UserGameParticipation.playForX
        : UserGameParticipation.playForO;
    _status = GameStatus.XTurn;
    notifyListeners();
  }

  void join(GameData gameData, UserGameParticipation participation) {
    //TODO: remove gamedata?
    _gameMap = gameData.gameMap;
    _generalInfo = gameData.generalInfo;
    _participation = participation;
    notifyListeners();
  }

  void view(GameData gameData) {
    //TODO: remove gamedata?
    _gameMap = gameData.gameMap;
    _generalInfo = gameData.generalInfo;
    _participation = UserGameParticipation.readOnly;
    notifyListeners();
  }
}

enum UserGameParticipation {
  readOnly,
  playForX,
  playForO,
}

class GameStatusDto {
  final GameStatus status;
  final List<List<CellStatus>> gameMap;

  GameStatusDto(this.status, this.gameMap);
}
