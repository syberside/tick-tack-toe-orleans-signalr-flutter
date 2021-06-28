import 'dart:async';

import 'package:clientapp/services/dtos/game_list_item_dto.dart';
import 'package:clientapp/services/dtos/game_status_dto.dart';
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
    connection.on('GameUpdated', _processUpdateMessage);

    _connection = connection;
  }

  void _processUpdateMessage(message) {
    _logger.d("Received updates: $message");
    var updateMessages = message as List<dynamic>;
    var updateItems = updateMessages.map((x) => GameStatusDto.fromJson(x));
    for (var item in updateItems) {
      _gameUpdatesCtrl.add(item);
    }
    _logger.d("Updates pushed to consumers");
  }

  Future<void> disconnect() async => await _connection?.stop();

  Future<String> login(String username) async {
    if (_connection == null) {
      throw Error();
    }
    var result = await _connection?.invoke("Login", args: [username]);
    return result as String;
  }

  Future<List<GameListItemDto>> getLobbies() async {
    if (_connection == null) {
      throw Error();
    }
    var result = await _connection?.invoke("GetLobbies");
    var items = result as List<dynamic>;
    var data = items.map((x) => GameListItemDto.fromJson(x)).toList();
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
    _logger.d("Created game: $result");
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

  Future<GameStatusDto> joinGame(String gameId, String authenticationToken) async {
    if (_connection == null) {
      throw Error();
    }
    var result = await _connection!.invoke("JoinGame", args: [gameId, authenticationToken]);
    _logger.d("Join game: $result");
    return GameStatusDto.fromJson(result);
  }
}
