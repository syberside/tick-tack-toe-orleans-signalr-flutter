import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;

class ApiConfig {
  String get gamesHubUrl {
    final host = kIsWeb
        ? 'localhost'
        : Platform.isAndroid
            ? '10.0.2.2'
            : 'localhost';
    var url = 'http://$host:5000/gamesHub';
    return url;
  }
}
