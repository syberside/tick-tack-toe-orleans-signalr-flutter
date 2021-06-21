import 'dart:async';
import 'dart:math';

import 'package:clientapp/data/cell_status.dart';
import 'package:clientapp/data/game_data.dart';
import 'package:clientapp/data/game_general_info.dart';
import 'package:clientapp/data/game_status.dart';
import 'package:clientapp/models/current_game_model.dart';
import 'package:clientapp/services/api.dart';

class ApiMock implements Api {
  static const String _authToken = 'token';
  String? _username;
  final List<String> _subscribedToGames = [];
  final List<String> _gamesWithBot = [];
  final Map<String, GameData> _games = {};
  StreamController<GameStatusDto> _updatesController = StreamController<GameStatusDto>.broadcast();

  static const String botName = 'BOT';

  @override
  Future<void> addBot(String gameId, String authenticationToken) {
    _gamesWithBot.add(gameId);
    var game = _games[gameId]!;
    _pushUpdate(game);

    var playForX = _isPlayerX(game);
    if (!playForX) {
      _makeOpponentRandomTurn(game, false);
    }

    return Future.value(null);
  }

  void _pushUpdate(GameData game) {
    var playForX = _isPlayerX(game);

    _updatesController.add(GameStatusDto(
      game.status,
      game.gameMap,
      playForX ? _username! : botName,
      playForX ? botName : _username!,
    ));
  }

  @override
  Future<void> connect() => Future.value(null);

  @override
  Future<String> createGame(String token, bool playForX) {
    var gameId = Random(DateTime.now().millisecond).nextDouble().toString();
    _games[gameId] = GameData.createdByUser(_username!, playForX, gameId);
    return Future.value(gameId);
  }

  @override
  Future<void> disconnect() => Future.value(null);

  @override
  Stream<GameStatusDto> get gameUpdates => _updatesController.stream;

  @override
  Future<List<GameGeneralInfo>> getLobbies() => Future.value(_games.values.map((x) => x.generalInfo).toList());

  @override
  Future<String> login(String username) {
    _username = username;
    // NOTE: adding games for testing purposes
    _games["1"] = GameData.createdByUser("OtherUser", true, "1");
    _games["2"] = GameData.createdByUser("OtherUser", false, "2");
    for (var value in GameStatus.values) {
      _games[value.toString()] = GameData(
        GameData.createEmptyMap(),
        GameGeneralInfo(value.toString(), "Bob", "Jack"),
        value,
      );
    }

    return Future.value(_authToken);
  }

  @override
  Future<void> subscribeForChanges(String gameId) {
    _subscribedToGames.add(gameId);
    return Future.value(null);
  }

  @override
  Future<void> turn(int x, int y, String authToken, String gameId) {
    var game = _games[gameId]!;
    var playForX = _isPlayerX(game);
    var userMark = playForX ? CellStatus.X : CellStatus.O;
    game.gameMap[x][y] = userMark;

    var isWin = game.gameMap[0].where((x) => x == userMark).length == 3;
    if (isWin) {
      game.status = playForX ? GameStatus.XWin : GameStatus.OWin;
      _pushUpdate(game);
      return Future.value(null);
    }

    _makeOpponentRandomTurn(game, playForX);
    return Future.value(null);
  }

  bool _isPlayerX(GameData game) {
    var playForX = game.generalInfo.playerX == _username;
    return playForX;
  }

  void _makeOpponentRandomTurn(GameData game, bool playForX) {
    var opponentMark = playForX ? CellStatus.O : CellStatus.X;

    var stepX = 0;
    var stepY = 0;
    for (var i = 2; i >= 0; i--) {
      for (var j = 2; j >= 0; j--) {
        if (game.gameMap[i][j] == CellStatus.Empty) {
          stepX = i;
          stepY = j;
          break;
        }
      }
      if (stepY + stepX != 0) {
        break;
      }
    }
    game.gameMap[stepX][stepY] = opponentMark;

    var isLose = game.gameMap[2].where((x) => x == opponentMark).length == 3;
    game.status = isLose
        ? playForX
            ? GameStatus.OWin
            : GameStatus.XWin
        : playForX
            ? GameStatus.XTurn
            : GameStatus.OTurn;
    _pushUpdate(game);
  }

  @override
  Future<GameStatusDto> joinGame(String gameId, String authenticationToken) async {
    var game = _games[gameId];
    if (game == null) {
      throw ArgumentError();
    }
    return GameStatusDto(
      game.status,
      game.gameMap,
      game.generalInfo.playerX,
      game.generalInfo.playerO,
    );
  }
}
