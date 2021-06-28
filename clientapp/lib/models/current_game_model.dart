import 'package:clientapp/data/cell_status.dart';
import 'package:clientapp/data/game_data.dart';
import 'package:clientapp/data/game_general_info.dart';
import 'package:clientapp/data/game_status.dart';
import 'package:clientapp/data/user_game_participation.dart';
import 'package:clientapp/services/dtos/game_status_dto.dart';
import 'package:flutter/foundation.dart';
import 'package:logger/logger.dart';

class CurrentGameModel extends ChangeNotifier {
  final Logger _logger;
  UserGameParticipation _participation = UserGameParticipation.readOnly;
  GameStatus? _status;
  List<List<CellStatus>> _gameMap = GameData.createEmptyMap();
  GameGeneralInfo? _generalInfo;

  CurrentGameModel(Stream<GameStatusDto> gameUpdates, this._logger) {
    gameUpdates.listen((event) {
      //TODO: filter for current game
      _gameMap = event.gameMap.data;
      _status = event.status;
      _generalInfo!.playerOName = event.playerOName;
      _generalInfo!.playerXName = event.playerXName;
      _logger.i("Update processed: $event");
      notifyListeners();
    });
  }

  UserGameParticipation get participation => _participation;
  GameStatus? get status => _status;
  List<List<CellStatus>> get gameMap => _gameMap;
  GameGeneralInfo? get generalInfo => _generalInfo;

  void makeOptimisticTurn(int i, int j) {
    _gameMap[i][j] = participation == UserGameParticipation.playForX ? CellStatus.X : CellStatus.O;
    _status = status == GameStatus.XTurn ? GameStatus.OTurn : GameStatus.XTurn;
    notifyListeners();
  }

  void newGameCreated(GameData gameData, bool playForX) {
    _gameMap = gameData.gameMap;
    _generalInfo = gameData.generalInfo;
    _participation = playForX ? UserGameParticipation.playForX : UserGameParticipation.playForO;
    _status = GameStatus.XTurn;
    notifyListeners();
  }

  void join(GameData gameData, UserGameParticipation participation) {
    _gameMap = gameData.gameMap;
    _generalInfo = gameData.generalInfo;
    _participation = participation;
    _status = gameData.status;
    notifyListeners();
  }

  void view(GameData gameData) {
    _gameMap = gameData.gameMap;
    _generalInfo = gameData.generalInfo;
    _participation = UserGameParticipation.readOnly;
    notifyListeners();
  }
}
