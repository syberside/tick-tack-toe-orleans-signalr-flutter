import 'package:clientapp/models/current_game_model.dart';

class GameGeneralInfo {
  bool get canParticipate => playerO == null || playerX == null;
  bool get isFilledWithPlayers => !canParticipate;

  String? playerX;
  String? playerO;
  String gameId;

  GameGeneralInfo(this.gameId, this.playerX, this.playerO);

  UserGameParticipation posibleParticipation() {
    if (playerX == null) {
      return UserGameParticipation.playForX;
    }
    if (playerO == null) {
      return UserGameParticipation.playForO;
    }
    return UserGameParticipation.readOnly;
  }
}
