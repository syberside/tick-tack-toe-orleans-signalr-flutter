import 'package:flutter/foundation.dart';

class AuthData with ChangeNotifier {
  String? _authToken;

  String? get authToken => _authToken;

  void storeToken(String token) {
    _authToken = token;
    notifyListeners();
  }
}
