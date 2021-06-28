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

  Future<String> login(String username) => _call("Login", [username], (x) => x as String);

  Future<List<GameListItemDto>> getLobbies() =>
      _call("GetLobbies", [], (r) => (r as List<dynamic>).map((x) => GameListItemDto.fromJson(x)).toList());

  Future<String> createGame(String token, bool playForX) => _call("CreateGame", [token, playForX], (x) => x as String);

  Future<void> subscribeForChanges(String gameId) => _call("Watch", [gameId], (x) => null);

  Future<void> turn(int x, int y, String authToken, String gameId) =>
      _call("Turn", [x, y, authToken, gameId], (x) => null);

  Future<void> addBot(String gameId, String authenticationToken) =>
      _call("AddBot", [gameId, authenticationToken], (x) => null);

  Future<GameStatusDto> joinGame(String gameId, String authenticationToken) =>
      _call("JoinGame", [gameId, authenticationToken], (x) => GameStatusDto.fromJson(x));

  Future<T> _call<T>(String method, List<Object?> args, T Function(dynamic) deserializer) async {
    if (_connection == null) {
      throw Error();
    }
    var result = await _connection!.invoke(method, args: args);
    _logger.d("$method response:  $result");
    return deserializer(result);
  }
}
