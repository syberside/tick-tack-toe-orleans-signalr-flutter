import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;

/// TODO: Replace with config file
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

  /// Set tru to use stub for BE integration
  bool diconnectedMode = true;
}
