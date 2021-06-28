import 'package:clientapp/data/user_game_participation.dart';

class GameGeneralInfo {
  bool get canParticipate => playerOName == null || playerXName == null;
  bool get isFilledWithPlayers => !canParticipate;

  String? playerXName;
  String? playerOName;
  String gameId;

  GameGeneralInfo(this.gameId, this.playerXName, this.playerOName);

  UserGameParticipation posibleParticipation() {
    if (playerXName == null) {
      return UserGameParticipation.playForX;
    }
    if (playerOName == null) {
      return UserGameParticipation.playForO;
    }
    return UserGameParticipation.readOnly;
  }
}
