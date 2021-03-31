import 'package:flutter/foundation.dart';

class CurrentGameModel extends ChangeNotifier {
  UserGameParticipation _participation = UserGameParticipation.readOnly;
  String? _gameId;

  UserGameParticipation get participation => _participation;
  String? get gameId => _gameId;
}

enum UserGameParticipation {
  readOnly,
  playForX,
  playForY,
}
