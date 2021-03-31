import 'package:flutter/foundation.dart';

class AuthData with ChangeNotifier {
  String? _authToken;
  String? _username;

  String? get authToken => _authToken;

  String? get username => _username;

  void update(String token, String username) {
    _authToken = token;
    _username = username;
    notifyListeners();
  }
}
