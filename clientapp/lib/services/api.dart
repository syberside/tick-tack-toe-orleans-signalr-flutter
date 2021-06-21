import 'dart:async';

import 'package:clientapp/models/current_game_model.dart';
import 'package:clientapp/pages/lobbies_page.dart';
import 'package:http/io_client.dart';
import 'package:logger/logger.dart';
import 'package:signalr_core/signalr_core.dart';
import 'dart:io';

import 'api_config.dart';

class Api {
  final Logger _logger;
  final ApiConfig _apiConfig;
  HubConnection? _connection;
  StreamController<GameStatusDto> _gameUpdatesCtrl = StreamController.broadcast(sync: false);

  Api(this._apiConfig, this._logger);

  Stream<GameStatusDto> get gameUpdates => _gameUpdatesCtrl.stream;

  Future<void> connect() async {
    String url = _apiConfig.gamesHubUrl;
    final connection = HubConnectionBuilder()
        .withUrl(
            url,
            HttpConnectionOptions(
              client: IOClient(HttpClient()..badCertificateCallback = (x, y, z) => true),
              logging: (level, message) => _logger.d(message),
            ))
        .build();

    await connection.start();
    connection.on('GameUpdated', (message) {
      //TODO: filter for current game
      _logger.i("Received update: $message");
      var u = (message as List<dynamic>).first;
      var m = u["gameMap"]["data"] as List<dynamic>;
      var data = GameStatusDto(
        GameStatus.values[u["status"] as int],
        m.map((r) => (r as List<dynamic>).map((x) => CellStatus.values[x as int]).toList()).toList(),
        u["playerXName"] as String,
        u["playerOName"] as String,
      );
      _gameUpdatesCtrl.add(data);
      _logger.i("Update resended");
    });

    _connection = connection;
  }

  Future<void> disconnect() async {
    await _connection?.stop();
  }

  Future<String> login(String username) async {
    if (_connection == null) {
      throw Error();
    }
    var result = await _connection?.invoke("Login", args: [username]);
    return result as String;
  }

  Future<List<GameGeneralInfo>> getLobbies() async {
    if (_connection == null) {
      throw Error();
    }
    var result = await _connection?.invoke("GetLobbies");
    //TODO: Add generated serialization (fromJson)
    var items = result as List<dynamic>;
    var data = items
        .map((x) => GameGeneralInfo(
              x["gameId"] as String,
              x["playerX"] is String ? x["playerX"] as String : null,
              x["playerO"] is String ? x["playerO"] as String : null,
            ))
        .toList();
    return data;
  }

  Future<String> createGame(String token, bool playForX) async {
    if (_connection == null) {
      throw Error();
    }
    var result = await _connection?.invoke("CreateGame", args: [
      token,
      playForX,
    ]);
    _logger.i("Created game: $result");
    return result as String;
  }

  Future<void> subscribeForChanges(String gameId) async {
    if (_connection == null) {
      throw Error();
    }
    await _connection!.invoke("Watch", args: [gameId]);
  }

  Future<void> turn(int x, int y, String authToken, String gameId) async {
    if (_connection == null) {
      throw Error();
    }
    await _connection!.invoke("Turn", args: [x, y, authToken, gameId]);
  }

  Future<void> addBot(String gameId, String authenticationToken) async {
    if (_connection == null) {
      throw Error();
    }
    await _connection!.invoke("AddBot", args: [gameId, authenticationToken]);
  }

  Future<bool> joinGame(String gameId, String authenticationToken) async {
    if (_connection == null) {
      throw Error();
    }
    var result = await _connection!.invoke("JoinGame", args: [gameId, authenticationToken]);
    return result;
  }
}
