import 'package:clientapp/data/cell_status.dart';
import 'package:clientapp/data/game_general_info.dart';
import 'package:clientapp/data/game_status.dart';

class GameData {
  List<List<CellStatus>> gameMap;
  GameGeneralInfo generalInfo;
  GameStatus status;

  GameData(this.gameMap, this.generalInfo, this.status);

  GameData.createdByUser(String username, bool playForX, String gameId)
      : this(
            createEmptyMap(),
            GameGeneralInfo(
              gameId,
              playForX ? username : null,
              playForX ? null : username,
            ),
            GameStatus.XTurn);

  static List<List<CellStatus>> createEmptyMap() => [
        [CellStatus.Empty, CellStatus.Empty, CellStatus.Empty],
        [CellStatus.Empty, CellStatus.Empty, CellStatus.Empty],
        [CellStatus.Empty, CellStatus.Empty, CellStatus.Empty],
      ];
}
