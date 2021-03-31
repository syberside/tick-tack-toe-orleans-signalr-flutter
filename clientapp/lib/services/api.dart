import 'package:clientapp/pages/lobbies_page.dart';
import 'package:http/io_client.dart';
import 'package:signalr_core/signalr_core.dart';
import 'dart:io';
import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;

class Api {
  HubConnection? _connection;

  Future<void> connect() async {
    final host = kIsWeb
        ? 'localhost'
        : Platform.isAndroid
            ? '10.0.2.2'
            : 'localhost';
    final connection = HubConnectionBuilder()
        .withUrl(
            'http://$host:5000/chatHub',
            HttpConnectionOptions(
              client: IOClient(
                  HttpClient()..badCertificateCallback = (x, y, z) => true),
              logging: (level, message) => print(message),
            ))
        .build();

    await connection.start();
    _connection = connection;
    // connection.on('GameUpdated', (message) {
    //   print("RECEIVED");
    //   print(message.toString());
    // });

    // await connection
    //     .invoke('Watch', args: ['364b499d-4d63-497b-b41a-d21469ad65a8']);
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
              x["playerX"] as String,
              x["playerO"] as String,
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
    return result as String;
  }
}
